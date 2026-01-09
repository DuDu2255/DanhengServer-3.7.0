using EggLink.DanhengServer.Database;
using EggLink.DanhengServer.Data; // 必须有这一行，才能找到 GameData
using EggLink.DanhengServer.Database.Friend;
using EggLink.DanhengServer.Database.Player;
using EggLink.DanhengServer.GameServer.Command;
using EggLink.DanhengServer.GameServer.Game.Player;
using EggLink.DanhengServer.GameServer.Server;
using EggLink.DanhengServer.GameServer.Server.Packet.Send.Chat;
using EggLink.DanhengServer.GameServer.Server.Packet.Send.Friend;
using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;
using EggLink.DanhengServer.Util;

namespace EggLink.DanhengServer.GameServer.Game.Friend;

public class FriendManager(PlayerInstance player) : BasePlayerManager(player)
{
    public FriendData FriendData { get; set; } =
        DatabaseHelper.Instance!.GetInstanceOrCreateNew<FriendData>(player.Uid);

    public async ValueTask<Retcode> AddFriend(int targetUid)
    {
        if (targetUid == Player.Uid) return Retcode.RetSucc; // Cannot add self
        if (FriendData.FriendDetailList.ContainsKey(targetUid)) return Retcode.RetFriendAlreadyIsFriend;
        if (FriendData.BlackList.Contains(targetUid)) return Retcode.RetFriendInBlacklist;
        if (FriendData.SendApplyList.Contains(targetUid)) return Retcode.RetSucc; // Already send apply

        var target = DatabaseHelper.Instance!.GetInstance<FriendData>(targetUid);
        if (target == null) return Retcode.RetFriendPlayerNotFound;
        if (target.BlackList.Contains(Player.Uid)) return Retcode.RetFriendInTargetBlacklist;
        if (target.ReceiveApplyList.Contains(targetUid)) return Retcode.RetSucc; // Already receive apply

        FriendData.SendApplyList.Add(targetUid);
        target.ReceiveApplyList.Add(Player.Uid);

        var targetPlayer = Listener.GetActiveConnection(targetUid);
        if (targetPlayer != null)
            await targetPlayer.SendPacket(new PacketSyncApplyFriendScNotify(Player.Data));

        DatabaseHelper.ToSaveUidList.Add(targetUid);
        return Retcode.RetSucc;
    }

    public async ValueTask<PlayerData?> ConfirmAddFriend(int targetUid)
    {
        if (targetUid == Player.Uid) return null; // Cannot add self
        if (FriendData.FriendDetailList.ContainsKey(targetUid)) return null;
        if (FriendData.BlackList.Contains(targetUid)) return null;

        var target = DatabaseHelper.Instance!.GetInstance<FriendData>(targetUid);
        var targetData = PlayerData.GetPlayerByUid(targetUid);
        if (target == null || targetData == null) return null;
        if (target.FriendDetailList.ContainsKey(Player.Uid)) return null;
        if (target.BlackList.Contains(Player.Uid)) return null;

        FriendData.ReceiveApplyList.Remove(targetUid);
        FriendData.FriendDetailList.Add(targetUid, new FriendDetailData());
        target.SendApplyList.Remove(Player.Uid);
        target.FriendDetailList.Add(Player.Uid, new FriendDetailData());

        var targetPlayer = Listener.GetActiveConnection(targetUid);
        if (targetPlayer != null)
            await targetPlayer.SendPacket(new PacketSyncHandleFriendScNotify((uint)Player.Uid, true, Player.Data));

        DatabaseHelper.ToSaveUidList.Add(targetUid);
        return targetData;
    }

    public async ValueTask RefuseAddFriend(int targetUid)
    {
        var target = DatabaseHelper.Instance!.GetInstance<FriendData>(targetUid);
        if (target == null) return;

        FriendData.ReceiveApplyList.Remove(targetUid);
        target.SendApplyList.Remove(Player.Uid);

        var targetPlayer = Listener.GetActiveConnection(targetUid);
        if (targetPlayer != null)
            await targetPlayer.SendPacket(new PacketSyncHandleFriendScNotify((uint)Player.Uid, false, Player.Data));

        DatabaseHelper.ToSaveUidList.Add(targetUid);
    }

    public async ValueTask<PlayerData?> AddBlackList(int targetUid)
    {
        var blackInfo = GetFriendPlayerData([targetUid]).First();
        var target = DatabaseHelper.Instance!.GetInstance<FriendData>(targetUid);
        if (blackInfo == null || target == null) return null;

        FriendData.FriendDetailList.Remove(targetUid);
        target.FriendDetailList.Remove(Player.Uid);
        if (!FriendData.BlackList.Contains(targetUid))
            FriendData.BlackList.Add(targetUid);

        var targetPlayer = Listener.GetActiveConnection(targetUid);
        if (targetPlayer != null)
            await targetPlayer.SendPacket(new PacketSyncAddBlacklistScNotify(Player.Uid));

        DatabaseHelper.ToSaveUidList.Add(targetUid);
        return blackInfo;
    }

    public void RemoveBlackList(int targetUid)
    {
        var target = DatabaseHelper.Instance!.GetInstance<FriendData>(targetUid);
        if (target == null) return;
        FriendData.BlackList.Remove(targetUid);
    }

    public async ValueTask<int?> RemoveFriend(int targetUid)
    {
        var target = DatabaseHelper.Instance!.GetInstance<FriendData>(targetUid);
        if (target == null) return null;

        FriendData.FriendDetailList.Remove(targetUid);
        target.FriendDetailList.Remove(Player.Uid);

        var targetPlayer = Listener.GetActiveConnection(targetUid);
        if (targetPlayer != null)
            await targetPlayer.SendPacket(new PacketSyncDeleteFriendScNotify(Player.Uid));

        DatabaseHelper.ToSaveUidList.Add(targetUid);
        return targetUid;
    }

    public async ValueTask SendMessage(int sendUid, int recvUid, string? message = null, int? extraId = null)
    {
        var data = new FriendChatData
        {
            SendUid = sendUid,
            ReceiveUid = recvUid,
            Message = message ?? "",
            ExtraId = extraId ?? 0,
            SendTime = Extensions.GetUnixSec()
        };

        if (!FriendData.ChatHistory.TryGetValue(recvUid, out var value))
        {
            FriendData.ChatHistory[recvUid] = new FriendChatHistory();
            value = FriendData.ChatHistory[recvUid];
        }

        value.MessageList.Add(data);

        PacketRevcMsgScNotify proto;
        if (message != null)
            proto = new PacketRevcMsgScNotify((uint)recvUid, (uint)sendUid, message);
        else
            proto = new PacketRevcMsgScNotify((uint)recvUid, (uint)sendUid, (uint)(extraId ?? 0));

        await Player.SendPacket(proto);

        if (recvUid == ConfigManager.Config.ServerOption.ServerProfile.Uid)
            if (message?.StartsWith('/') == true)
            {
                var cmd = message[1..];
                CommandExecutor.ExecuteCommand(new PlayerCommandSender(Player), cmd);
            }

        // receive message
        var recvPlayer = Listener.GetActiveConnection(recvUid)?.Player;
        if (recvPlayer != null)
        {
            await recvPlayer.FriendManager!.ReceiveMessage(sendUid, recvUid, message, extraId);
        }
        else
        {
            // offline
            var friendData = DatabaseHelper.Instance!.GetInstance<FriendData>(recvUid);
            if (friendData == null) return; // not exist maybe server profile
            if (!friendData.ChatHistory.TryGetValue(sendUid, out var history))
            {
                friendData.ChatHistory[sendUid] = new FriendChatHistory();
                history = friendData.ChatHistory[sendUid];
            }

            history.MessageList.Add(data);

            DatabaseHelper.ToSaveUidList.Add(recvUid);
        }
    }

    public async ValueTask SendInviteMessage(int sendUid, int recvUid, LobbyInviteInfo info)
    {
        var proto = new PacketRevcMsgScNotify((uint)recvUid, (uint)sendUid, info);
        await Player.SendPacket(proto);

        // receive message
        var recvPlayer = Listener.GetActiveConnection(recvUid)?.Player;
        if (recvPlayer != null) await recvPlayer.FriendManager!.ReceiveInviteMessage(sendUid, recvUid, info);
    }

    public async ValueTask ReceiveMessage(int sendUid, int recvUid, string? message = null, int? extraId = null)
    {
        var data = new FriendChatData
        {
            SendUid = sendUid,
            ReceiveUid = recvUid,
            Message = message ?? "",
            ExtraId = extraId ?? 0,
            SendTime = Extensions.GetUnixSec()
        };

        if (!FriendData.ChatHistory.TryGetValue(sendUid, out var value))
        {
            FriendData.ChatHistory[sendUid] = new FriendChatHistory();
            value = FriendData.ChatHistory[sendUid];
        }

        value.MessageList.Add(data);

        PacketRevcMsgScNotify proto;
        if (message != null)
            proto = new PacketRevcMsgScNotify((uint)recvUid, (uint)sendUid, message);
        else
            proto = new PacketRevcMsgScNotify((uint)recvUid, (uint)sendUid, (uint)(extraId ?? 0));

        await Player.SendPacket(proto);
    }

    public async ValueTask ReceiveInviteMessage(int sendUid, int recvUid, LobbyInviteInfo info)
    {
        var proto = new PacketRevcMsgScNotify((uint)recvUid, (uint)sendUid, info);

        await Player.SendPacket(proto);
    }

    public FriendDetailData? GetFriendDetailData(int uid)
    {
        if (uid == ConfigManager.Config.ServerOption.ServerProfile.Uid)
            return new FriendDetailData { IsMark = true };

        if (!FriendData.FriendDetailList.TryGetValue(uid, out var friend)) return null;

        return friend;
    }

    public List<ChatMessageData> GetHistoryInfo(int uid)
    {
        if (!FriendData.ChatHistory.TryGetValue(uid, out var history)) return [];

        var info = new List<ChatMessageData>();

        foreach (var chat in history.MessageList)
            info.Add(new ChatMessageData
            {
                CreateTime = (ulong)chat.SendTime,
                Content = chat.Message,
                ExtraId = (uint)chat.ExtraId,
                SenderId = (uint)chat.SendUid,
                MessageType = chat.ExtraId > 0 ? MsgType.Emoji : MsgType.CustomText
            });

        info.Reverse();

        return info;
    }

    public List<PlayerData> GetFriendPlayerData(List<int>? uids = null)
    {
        var list = new List<PlayerData>();
        uids ??= [.. FriendData.FriendDetailList.Keys];

        foreach (var friend in uids)
        {
            var player = PlayerData.GetPlayerByUid(friend);
            if (player != null) list.Add(player);
        }

        var serverProfile = ConfigManager.Config.ServerOption.ServerProfile;
        list.Add(new PlayerData
        {
            Uid = serverProfile.Uid,
            HeadIcon = serverProfile.HeadIcon,
            Signature = serverProfile.Signature,
            Level = serverProfile.Level,
            WorldLevel = 0,
            Name = serverProfile.Name,
            ChatBubble = serverProfile.ChatBubbleId,
            PersonalCard = serverProfile.PersonalCardId
        });

        return list;
    }

    public List<PlayerData> GetBlackList()
    {
        List<PlayerData> list = [];

        foreach (var friend in FriendData.BlackList)
        {
            var player = PlayerData.GetPlayerByUid(friend);

            if (player != null) list.Add(player);
        }

        return list;
    }

    public List<PlayerData> GetSendApplyList()
    {
        List<PlayerData> list = [];

        foreach (var friend in FriendData.SendApplyList)
        {
            var player = PlayerData.GetPlayerByUid(friend);

            if (player != null) list.Add(player);
        }

        return list;
    }

    public List<PlayerData> GetReceiveApplyList()
    {
        List<PlayerData> list = [];

        foreach (var friend in FriendData.ReceiveApplyList)
        {
            var player = PlayerData.GetPlayerByUid(friend);

            if (player != null) list.Add(player);
        }

        return list;
    }

    public List<PlayerData> GetRandomFriend()
    {
        var list = new List<PlayerData>();

        foreach (var kcp in DanhengListener.Connections.Values)
        {
            if (kcp.State != SessionStateEnum.ACTIVE) continue;
            if (kcp is not Connection connection) continue;
            if (connection.Player?.Uid == Player.Uid) continue;
            var data = connection.Player?.Data;
            if (data == null) continue;
            list.Add(data);
        }

        return list.Take(20).ToList();
    }

    public void RemarkFriendName(int uid, string remarkName)
    {
        if (!FriendData.FriendDetailList.TryGetValue(uid, out var friend)) return;
        friend.RemarkName = remarkName;
    }

    public void MarkFriend(int uid, bool isMark)
    {
        if (!FriendData.FriendDetailList.TryGetValue(uid, out var friend)) return;
        friend.IsMark = isMark;
    }
 // 建议修改函数签名，传入客户端请求的 Type
public GetFriendRecommendLineupScRsp GetGlobalRecommendLineup(uint challengeId, uint requestType) 
{
    var Log = Logger.GetByClassName();
    Log.Info($"[LOG-DEBUG] === 战报请求开始 ===");
    Log.Info($"[LOG-DEBUG] 请求关卡: {challengeId} | 类型: {requestType} | UID: {Player.Uid}");

    var rsp = new GetFriendRecommendLineupScRsp
    {
        Key = challengeId,
        Retcode = 0,
        Type = (DLLLEANDAIH)requestType, 
        ONOCJEEBFCI = false    
    };

    if (!GameData.ChallengeConfigData.TryGetValue((int)challengeId, out var config))
    {
        Log.Error($"[LOG-DEBUG] 找不到配置 ID: {challengeId}");
        return rsp;
    }

    var allRecords = DatabaseHelper.sqlSugarScope?.Queryable<FriendRecordData>().ToList() ?? new();

    foreach (var record in allRecords)
    {
        var pData = PlayerData.GetPlayerByUid(record.Uid);
        if (pData == null) continue;

        bool isSelf = (record.Uid == Player.Uid);

        if (!record.ChallengeGroupStatistics.TryGetValue((uint)config.GroupID, out var groupStat)) continue;

        if (groupStat.MemoryGroupStatistics == null || 
            !groupStat.MemoryGroupStatistics.TryGetValue(challengeId, out var memoryStats)) continue;

        var entry = new KEHMGKIHEFN();
        entry.PlayerInfo = pData.ToSimpleProto(isSelf ? FriendOnlineStatus.Online : FriendOnlineStatus.Offline);

        if (isSelf)
        {
            Log.Info($"[LOG-DEBUG] [自己] 匹配成功，开始填充战报容器...");
            rsp.ONOCJEEBFCI = true; 
            
            // 修正处：根据你提供的源码，这里没有 Id 字段
            entry.GIEIDJEEPAC = new FCNOLLFGPCK
            {
                PlayerInfo = entry.PlayerInfo,
                CurLevelStars = memoryStats.Stars,
                ScoreId = memoryStats.RoundCount, 
                BuffOne = (uint)config.MazeBuffID,
                BuffTwo = (uint)config.MazeBuffID,
                RemarkName = "" // 源码中有此字段，设为空即可
            };

            // 填充详细阵容 (必须要转换模型)
            foreach (var dbTeam in memoryStats.Lineups)
            {
                var teamProto = new ChallengeLineupList();
                foreach (var av in dbTeam)
                {
                    teamProto.AvatarList.Add(new ChallengeAvatarInfo
                    {
                        Id = av.Id,
                        Level = av.Level,
                        Index = av.Index,
                        AvatarType = AvatarType.AvatarFormalType, 
                        GGDIIBCDOBB = av.Rank 
                    });
                }
                entry.GIEIDJEEPAC.LineupList.Add(teamProto);
            }
        }
        else
        {
            entry.PMHIBHNEPHI = BuildMemoryContainer(memoryStats, challengeId);
            entry.ADDCJEJPFEF = new KAMCIOPBPGA
            {
                PeakTargetList = { memoryStats.Stars, memoryStats.RoundCount }
            };
        }

        rsp.ChallengeRecommendList.Add(entry);
    }

    Log.Info($"[LOG-DEBUG] === 处理完毕 === ONOCJEEBFCI: {rsp.ONOCJEEBFCI}");
    return rsp;
}
  private DKHENLMAEBE BuildMemoryContainer(MemoryGroupStatisticsPb stats, uint cid)
{
    var container = new DKHENLMAEBE();
    
    // stats.Lineups 是 List<List<ChallengeAvatarInfoPb>>
    foreach (var team in stats.Lineups)
    {
        var sideProto = new GIIHBKMJKHM { PeakLevelId = cid };
        
        foreach (var avPb in team)
        {
            sideProto.AvatarList.Add(new OILPIACENNH
            {
                AvatarType = avPb.AvatarType,
                Id = avPb.Id,
                Level = avPb.Level,
                Index = avPb.Index,
                GGDIIBCDOBB = avPb.Rank // 刚才已经在 FriendRecordData 里补全了 Rank 属性
            });
        }
        container.HFPPEGIFFLM.Add(sideProto);
    }
    return container;
}
    public GetFriendListInfoScRsp ToProto()
    {
        var proto = new GetFriendListInfoScRsp();

        foreach (var player in GetFriendPlayerData())
        {
            var status = Listener.GetActiveConnection(player.Uid) == null
                ? FriendOnlineStatus.Offline
                : FriendOnlineStatus.Online;
            var friend = GetFriendDetailData(player.Uid) ?? new FriendDetailData();

            proto.FriendList.Add(new FriendSimpleInfo
            {
                PlayerInfo = player.ToSimpleProto(status),
                IsMarked = friend.IsMark,
                RemarkName = friend.RemarkName
            });
        }

        foreach (var player in GetBlackList())
        {
            var status = Listener.GetActiveConnection(player.Uid) == null
                ? FriendOnlineStatus.Offline
                : FriendOnlineStatus.Online;
            proto.BlackList.Add(player.ToSimpleProto(status));
        }

        return proto;
    }

    public GetFriendApplyListInfoScRsp ToApplyListProto()
    {
        GetFriendApplyListInfoScRsp proto = new();

        foreach (var player in GetSendApplyList()) proto.SendApplyList.Add((uint)player.Uid);

        foreach (var player in GetReceiveApplyList())
        {
            var status = Listener.GetActiveConnection(player.Uid) == null
                ? FriendOnlineStatus.Offline
                : FriendOnlineStatus.Online;
            proto.ReceiveApplyList.Add(new FriendApplyInfo
            {
                PlayerInfo = player.ToSimpleProto(status)
            });
        }

        return proto;
    }
}
