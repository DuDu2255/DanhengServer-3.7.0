using EggLink.DanhengServer.Data;
using EggLink.DanhengServer.Database.Challenge;
using EggLink.DanhengServer.Database.Lineup;
using EggLink.DanhengServer.GameServer.Game.Challenge.Instances;
using EggLink.DanhengServer.GameServer.Game.Player;
using EggLink.DanhengServer.GameServer.Server.Packet.Send.ChallengePeak;
using EggLink.DanhengServer.Proto;
using EggLink.DanhengServer.Proto.ServerSide;
using EggLink.DanhengServer.Util;
using ChallengePeakLevelInfo = EggLink.DanhengServer.Proto.ChallengePeakLevelInfo;

namespace EggLink.DanhengServer.GameServer.Game.ChallengePeak;

/// <summary>
///     this class is used to manage the challenge peak for a player
///     but the challenge instance shouldnt be stored here ( see ChallengeManager )
/// </summary>
/// <see cref="EggLink.DanhengServer.GameServer.Game.Challenge.ChallengeManager" />
public class ChallengePeakManager(PlayerInstance player) : BasePlayerManager(player)
{
    public bool BossIsHard { get; set; } = true;

  public ChallengePeakLevelInfo GetChallengePeakInfo(int groupId)
{
    var proto = new ChallengePeakLevelInfo
    {
        PeakGroupId = (uint)groupId
    };

    // 1. 获取当前期的配置组数据
    var data = GameData.ChallengePeakGroupConfigData.GetValueOrDefault(groupId);
    if (data == null) return proto;

    var starNum = 0;
    // 关键变量：标记前一个关卡是否已通关，初始为 true 确保第一关显示
    bool isPrevFinished = true; 

    // 2. 处理普通关卡 (PreLevel)
    foreach (var levelId in data.PreLevelIDList)
    {
        var levelData = GameData.ChallengePeakConfigData.GetValueOrDefault(levelId);
        if (levelData == null) continue;

        // --- 自动化解锁逻辑 ---
        // 如果前一关没打通，后续所有关卡都不下发给客户端
        if (!isPrevFinished) continue; 

        var levelProto = new ChallengePeakPreLevel
        {
            PeakLevelId = (uint)levelId,
            IsFinished = false // 默认设为未完成
        };

        // 检查数据库中是否存在该关卡的通关记录
        if (Player.ChallengeManager!.ChallengeData.PeakLevelDatas.TryGetValue(levelId, out var levelPbData))
        {
            // 玩家已通关该关卡
            levelProto.IsFinished = true;
            isPrevFinished = true; // 允许循环继续处理下一关

            starNum += (int)levelPbData.PeakStar;
            levelProto.PeakRoundsCount = levelPbData.RoundCnt;
            levelProto.PeakLevelAvatarIdList.AddRange(levelPbData.BaseAvatarList);
            levelProto.PeakTargetList.AddRange(levelPbData.FinishedTargetList);

            // 填充角色信息供客户端展示
            foreach (var avatarId in levelPbData.BaseAvatarList)
            {
                var avatar = Player.AvatarManager!.GetFormalAvatar((int)avatarId);
                if (avatar == null) continue;
                levelProto.PeakAvatarInfoList.Add(avatar.ToPeakAvatarProto());
            }

            proto.FinishedPreNum++;
        }
        else
        {
            // 玩家未通关该关卡，标记后，循环中的后续关卡将被跳过
            levelProto.IsFinished = false;
            isPrevFinished = false; 
        }

        proto.PeakPreLevelInfoList.Add(levelProto);
    }

    proto.PreLevelStars = (uint)starNum;

    // --- 3. Boss 关卡解锁逻辑 ---
    // 只有当前置关卡（PreLevel）全部打通后，才继续处理并下发 Boss 关卡数据
    if (proto.FinishedPreNum < data.PreLevelIDList.Count) 
    {
        return proto; 
    }

    var bossLevelId = data.BossLevelID;
    if (bossLevelId <= 0) return proto;

    var bossLevelData = GameData.ChallengePeakBossConfigData.GetValueOrDefault(bossLevelId);
    if (bossLevelData == null) return proto;

    // 构建 Boss 关卡协议数据
    var bossProto = new ChallengePeakBossLevel
    {
        PeakBossLevelId = (uint)bossLevelId,
        IsUltraBossWin = false, // 默认未赢得终极挑战
        PeakEasyBoss = new ChallengePeakBossInfo(),
        PeakHardBoss = new ChallengePeakBossInfo()
    };

    HashSet<uint> targetIds = [];
    
    // 读取简单难度 Boss 记录 (is hard = 0)
    if (Player.ChallengeManager!.ChallengeData.PeakBossLevelDatas.TryGetValue((bossLevelId << 2) | 0, out var bossPbData))
    {
        bossProto.PeakEasyBoss.PeakLevelAvatarIdList.AddRange(bossPbData.BaseAvatarList);
        bossProto.PeakEasyBoss.BossDisplayAvatarIdList.AddRange(bossPbData.BaseAvatarList);
        bossProto.PeakEasyBoss.LeastRoundsCount = bossPbData.RoundCnt;
        bossProto.PeakEasyBoss.IsFinished = true;
        bossProto.PeakEasyBoss.BuffId = bossPbData.BuffId;
        foreach (var targetId in bossPbData.FinishedTargetList) targetIds.Add(targetId);
    }

    // 读取困难难度 Boss 记录 (is hard = 1)
    if (Player.ChallengeManager!.ChallengeData.PeakBossLevelDatas.TryGetValue((bossLevelId << 2) | 1, out var bossHardPbData))
    {
        bossProto.IsUltraBossWin = true;
        bossProto.PeakHardBoss.PeakLevelAvatarIdList.AddRange(bossHardPbData.BaseAvatarList);
        bossProto.PeakHardBoss.BossDisplayAvatarIdList.AddRange(bossHardPbData.BaseAvatarList);
        bossProto.PeakHardBoss.LeastRoundsCount = bossHardPbData.RoundCnt;
        bossProto.PeakHardBoss.IsFinished = true;
        bossProto.PeakHardBoss.BuffId = bossHardPbData.BuffId;
        foreach (var targetId in bossHardPbData.FinishedTargetList) targetIds.Add(targetId);
    }

    bossProto.PeakTargetList.AddRange(targetIds);
    proto.PeakBossLevel = bossProto;

    return proto;
}

    public async ValueTask SetLineupAvatars(int groupId, List<ChallengePeakLineup> lineups)
    {
        var datas = Player.ChallengeManager!.ChallengeData.PeakLevelDatas;
        foreach (var lineup in lineups)
        {
            List<uint> avatarIds = [];

            foreach (var avatarId in lineup.PeakLevelAvatarIdList.ToList())
            {
                var avatar = Player.AvatarManager!.GetFormalAvatar((int)avatarId);
                if (avatar != null)
                    avatarIds.Add((uint)avatar.BaseAvatarId);
            }

            datas[(int)lineup.PeakLevelId] = new ChallengePeakLevelData
            {
                LevelId = (int)lineup.PeakLevelId,
                BaseAvatarList = avatarIds
            }; // reset
        }

        await Player.SendPacket(new PacketChallengePeakGroupDataUpdateScNotify(GetChallengePeakInfo(groupId)));
    }

    public async ValueTask SaveHistory(ChallengePeakInstance inst, List<uint> targetIds)
    {
        if (inst.Config.BossExcel != null)
        {
            // is hard
            var isHard = inst.Data.Peak.IsHard;
            var levelId = ((int)inst.Data.Peak.CurrentPeakLevelId << 2) | (isHard ? 1 : 0);

            // get old data
            if (Player.ChallengeManager!.ChallengeData.PeakBossLevelDatas.TryGetValue(levelId, out var oldData) &&
                oldData.FinishedTargetList.Count > targetIds.Count && oldData.RoundCnt < inst.Data.Peak.RoundCnt)
                // better data already exists, do not overwrite
                return;

            // Save boss data
            var data = new ChallengePeakBossLevelData
            {
                LevelId = (int)inst.Data.Peak.CurrentPeakLevelId,
                IsHard = isHard,
                BaseAvatarList = Player.LineupManager!.GetCurLineup()?.BaseAvatars?.Select(x => (uint)x.BaseAvatarId)
                    .ToList() ?? [],
                RoundCnt = inst.Data.Peak.RoundCnt,
                BuffId = inst.Data.Peak.Buffs.FirstOrDefault(),
                FinishedTargetList = targetIds,
                PeakStar = (uint)targetIds.Count
            };

            Player.ChallengeManager!.ChallengeData.PeakBossLevelDatas[levelId] = data;

            // set head frame
            if (isHard)
            {
                await Player.SetPlayerHeadFrameId(GameConstants.CHALLENGE_PEAK_ULTRA_FRAME_ID, long.MaxValue);
            }
            else
            {
                var targetFrameId = data.PeakStar + 226000;
                if (Player.Data.HeadFrame.HeadFrameId < targetFrameId)
                    await Player.SetPlayerHeadFrameId(targetFrameId, long.MaxValue);
            }
        }
        else
        {
            // Save level data
            var levelId = (int)inst.Data.Peak.CurrentPeakLevelId;

            // get old data
            if (Player.ChallengeManager!.ChallengeData.PeakLevelDatas.TryGetValue(levelId, out var oldData) &&
                oldData.FinishedTargetList.Count > targetIds.Count && oldData.RoundCnt < inst.Data.Peak.RoundCnt)
                // better data already exists, do not overwrite
                return;

            var data = new ChallengePeakLevelData
            {
                LevelId = levelId,
                BaseAvatarList = Player.LineupManager!.GetCurLineup()?.BaseAvatars?.Select(x => (uint)x.BaseAvatarId)
                    .ToList() ?? [],
                RoundCnt = inst.Data.Peak.RoundCnt,
                FinishedTargetList = targetIds,
                PeakStar = (uint)targetIds.Count
            };

            Player.ChallengeManager!.ChallengeData.PeakLevelDatas[levelId] = data;
        }

        await Player.SendPacket(
            new PacketChallengePeakGroupDataUpdateScNotify(
                GetChallengePeakInfo((int)inst.Data.Peak.CurrentPeakGroupId)));
    }

    public async ValueTask StartChallenge(int levelId, uint buffId, List<int> avatarIdList)
    {
        // Get challenge excel
        if (!GameData.ChallengePeakConfigData.TryGetValue(levelId, out var excel))
        {
            await Player.SendPacket(new PacketStartChallengePeakScRsp(Retcode.RetChallengeNotExist));
            return;
        }

        // Format to base avatar id
        List<int> avatarIds = [];
        foreach (var avatarId in avatarIdList)
        {
            var avatar = Player.AvatarManager!.GetFormalAvatar(avatarId);
            if (avatar != null)
                avatarIds.Add(avatar.BaseAvatarId);
        }

        // Get lineup
        var lineup = Player.LineupManager!.GetExtraLineup(ExtraLineupType.LineupChallenge)!;
        if (avatarIds.Count > 0)
            lineup.BaseAvatars = avatarIds.Select(x => new LineupAvatarInfo
            {
                BaseAvatarId = x
            }).ToList();
        else
            lineup.BaseAvatars = Player.ChallengeManager!.ChallengeData.PeakLevelDatas.GetValueOrDefault(levelId)
                ?.BaseAvatarList
                .Select(x => new LineupAvatarInfo
                {
                    BaseAvatarId = (int)x
                }).ToList() ?? [];

        // Set technique points to full
        lineup.Mp = 8; // Max Mp

        // Make sure this lineup has avatars set
        if (Player.AvatarManager!.AvatarData.FormalAvatars.Count == 0)
        {
            await Player.SendPacket(new PacketStartChallengePeakScRsp(Retcode.RetChallengeLineupEmpty));
            return;
        }

        // Reset hp/sp
        foreach (var avatar in Player.AvatarManager!.AvatarData.FormalAvatars)
        {
            avatar.SetCurHp(10000, true);
            avatar.SetCurSp(5000, true);
        }

        // Set challenge data for player
        var data = new ChallengeDataPb
        {
            Peak = new ChallengePeakDataPb
            {
                CurrentPeakGroupId = (uint)(GameData.ChallengePeakGroupConfigData.Values
                    .FirstOrDefault(x => x.BossLevelID == levelId || x.PreLevelIDList.Contains(levelId))?.ID ?? 1),
                CurrentPeakLevelId = (uint)levelId,
                CurrentExtraLineup = ChallengeLineupTypePb.Challenge1,
                CurStatus = 1
            }
        };

        if (excel.BossExcel != null)
            data.Peak.IsHard = BossIsHard;

        if (buffId > 0) data.Peak.Buffs.Add(buffId);

        var instance = new ChallengePeakInstance(Player, data);

        Player.ChallengeManager!.ChallengeInstance = instance;

        // Set first lineup before we enter scenes
        await Player.LineupManager!.SetExtraLineup((ExtraLineupType)instance.Data.Peak.CurrentExtraLineup);

        // Enter scene
        try
        {
            await Player.EnterScene((int)GameConstants.CHALLENGE_PEAK_TARGET_ENTRY_ID[GameConstants.CHALLENGE_PEAK_CUR_GROUP_ID][0], 0, true);
        }
        catch
        {
            // Reset lineup/instance if entering scene failed
            Player.ChallengeManager!.ChallengeInstance = null;

            // Send error packet
            await Player.SendPacket(new PacketStartChallengePeakScRsp(Retcode.RetChallengeNotExist));
            return;
        }

        // Save start positions
        data.Peak.StartPos = Player.Data.Pos!.ToVector3Pb();
        data.Peak.StartPos = Player.Data.Rot!.ToVector3Pb();
        data.Peak.SavedMp = (uint)Player.LineupManager.GetCurLineup()!.Mp;

        // Send packet
        await Player.SendPacket(new PacketStartChallengePeakScRsp(Retcode.RetSucc));

        // Save instance
        Player.ChallengeManager!.SaveInstance(instance);
    }
}
