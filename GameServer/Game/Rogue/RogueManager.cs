using EggLink.DanhengServer.Data;
using EggLink.DanhengServer.Data.Excel;
using EggLink.DanhengServer.GameServer.Game.Player;
using EggLink.DanhengServer.GameServer.Server.Packet.Send.Lineup;
using EggLink.DanhengServer.GameServer.Server.Packet.Send.Rogue;
using EggLink.DanhengServer.Proto;
using EggLink.DanhengServer.Util;

namespace EggLink.DanhengServer.GameServer.Game.Rogue;

public class RogueManager(PlayerInstance player) : BasePlayerManager(player)
{
    #region Properties

    public RogueInstance? RogueInstance { get; set; }

    #endregion

    #region Information

    /// <summary>
    ///     Get the beginning time and end time
    /// </summary>
    /// <returns></returns>
    public static (long, long) GetCurrentRogueTime()
    {
        // get the first day of the week
        var beginTime = DateTime.Now.AddDays(-(int)DateTime.Now.DayOfWeek).AddHours(4);
        var endTime = beginTime.AddDays(7);
        return (beginTime.ToUnixSec(), endTime.ToUnixSec());
    }

    public int GetRogueScore()
    {
        return 0;
        // TODO: Implement
    }

    public void AddRogueScore(int score)
    {
    }

    public static RogueManagerExcel? GetCurrentManager()
    {
        foreach (var manager in GameData.RogueManagerData.Values)
            if (DateTime.Now >= manager.BeginTimeDate && DateTime.Now <= manager.EndTimeDate)
                return manager;
        return null;
    }

    #endregion

    #region Actions

    public async ValueTask StartRogue(int areaId, int aeonId, List<int> disableAeonId, List<int> baseAvatarIds)
    {
        if (GetRogueInstance() != null) return;
        GameData.RogueAreaConfigData.TryGetValue(areaId, out var area);
        GameData.RogueAeonData.TryGetValue(aeonId, out var aeon);

        if (area == null || aeon == null) return;

        Player.LineupManager!.SetExtraLineup(ExtraLineupType.LineupRogue, baseAvatarIds);
        await Player.LineupManager!.GainMp(8, false);
        await Player.SendPacket(new PacketSyncLineupNotify(Player.LineupManager!.GetCurLineup()!));

        foreach (var id in baseAvatarIds)
        {
            Player.AvatarManager!.GetFormalAvatar(id)?.SetCurHp(10000, true);
            Player.AvatarManager!.GetFormalAvatar(id)?.SetCurSp(5000, true);
        }

        RogueInstance = new RogueInstance(area, aeon, Player);
        await RogueInstance.EnterRoom(RogueInstance.StartSiteId);

        await Player.SendPacket(new PacketSyncRogueStatusScNotify(RogueInstance.Status));
        await Player.SendPacket(new PacketStartRogueScRsp(Player));
    }

    public BaseRogueInstance? GetRogueInstance()
    {
        if (RogueInstance != null)
            return RogueInstance;

        if (Player.ChessRogueManager?.RogueInstance != null)
            return Player.ChessRogueManager.RogueInstance;

        if (Player.RogueMagicManager?.RogueMagicInstance != null)
            return Player.RogueMagicManager.RogueMagicInstance;

        return Player.RogueTournManager?.RogueTournInstance;
    }

    #endregion

    #region Serialization

    public RogueInfo ToProto()
    {
        var proto = new RogueInfo
        {
            RogueGetInfo = ToGetProto()
        };

        if (RogueInstance != null) proto.RogueCurrentInfo = RogueInstance.ToProto();

        return proto;
    }

    public RogueGetInfo ToGetProto()
    {
        return new RogueGetInfo
        {
            RogueScoreRewardInfo = ToRewardProto(),
            RogueAeonInfo = ToAeonInfo(),
            RogueSeasonInfo = ToSeasonProto(),
            RogueAreaInfo = ToAreaProto(),
            RogueVirtualItemInfo = ToVirtualItemProto()
        };
    }

    public RogueScoreRewardInfo ToRewardProto()
    {
        var time = GetCurrentRogueTime();

        return new RogueScoreRewardInfo
        {
            ExploreScore = (uint)GetRogueScore(),
            PoolRefreshed = true,
            PoolId = (uint)(20 + Player.Data.WorldLevel),
            RewardBeginTime = time.Item1,
            RewardEndTime = time.Item2,
            HasTakenInitialScore = true
        };
    }

    public static RogueAeonInfo ToAeonInfo()
    {
        var proto = new RogueAeonInfo
        {
            IsUnlocked = true,
            UnlockedAeonNum = (uint)GameData.RogueAeonData.Count,
            UnlockedAeonEnhanceNum = 3
        };

        proto.AeonIdList.AddRange(GameData.RogueAeonData.Keys.Select(x => (uint)x));

        return proto;
    }

    public static RogueSeasonInfo ToSeasonProto()
    {
        var manager = GetCurrentManager();
        if (manager == null) return new RogueSeasonInfo();

        return new RogueSeasonInfo
        {
            Season = (uint)manager.RogueSeason,
            BeginTime = manager.BeginTimeDate.ToUnixSec(),
            EndTime = manager.EndTimeDate.ToUnixSec()
        };
    }

   // 1. 修改 ToAreaProto 逻辑
public RogueAreaInfo ToAreaProto()
{
    var manager = GetCurrentManager();
    if (manager == null) return new RogueAreaInfo();

    var proto = new RogueAreaInfo();
    foreach (var areaId in manager.RogueAreaIDList)
    {
        proto.RogueAreaList.Add(new RogueArea
        {
            AreaId = (uint)areaId,
            AreaStatus = GetAreaStatus(areaId), // 调用动态判定
            HasTakenReward = Player.Data.RogueData.TakenRewardIds.Contains(areaId)
        });
    }
    return proto;
}

// 2. 新增动态判定方法
public RogueAreaStatus GetAreaStatus(int areaId)
{
    // 已通关
    if (Player.Data.RogueData.FinishedAreaIds.Contains(areaId))
        return RogueAreaStatus.FirstPass;

    // 初始开启：世界1, 2, 3-难度1
    if (areaId == 110 || areaId == 120 || areaId == 130)
        return RogueAreaStatus.Unlock;

    // 难度递进解锁 (如 131 需要 130 通关)
    if (areaId % 10 > 0 && Player.Data.RogueData.FinishedAreaIds.Contains(areaId - 1))
        return RogueAreaStatus.Unlock;

    // 跨世界解锁 (如 140 需要 130 通关)
    if (areaId % 10 == 0 && Player.Data.RogueData.FinishedAreaIds.Contains(areaId - 10))
        return RogueAreaStatus.Unlock;

    return RogueAreaStatus.Lock;
}

    public static RogueGetVirtualItemInfo ToVirtualItemProto()
    {
        return new RogueGetVirtualItemInfo
        {
            // TODO: Implement
        };
    }

    public static RogueTalentInfoList ToTalentProto()
    {
        var proto = new RogueTalentInfoList();

        foreach (var talent in GameData.RogueTalentData)
            proto.TalentInfo.Add(new RogueTalentInfo
            {
                TalentId = (uint)talent.Key,
                Status = RogueTalentStatus.Enable
            });

        return proto;
    }

    #endregion
}
