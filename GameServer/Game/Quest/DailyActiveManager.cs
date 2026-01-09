using EggLink.DanhengServer.Database;
using EggLink.DanhengServer.Database.Quests;
using EggLink.DanhengServer.GameServer.Game.Player;
using EggLink.DanhengServer.GameServer.Server.Packet.Send.PlayerSync; 
using EggLink.DanhengServer.Proto;
using EggLink.DanhengServer.Util; 

namespace EggLink.DanhengServer.GameServer.Game.Quest;

public class DailyActiveManager(PlayerInstance player) : BasePlayerManager(player)
{
    // 获取 Logger 实例以修复报错 3
    private static readonly Logger Log = Logger.GetByClassName();

    public DailyActiveData Data => 
        DatabaseHelper.Instance!.GetInstanceOrCreateNew<DailyActiveData>(Player.Uid);

    public GetDailyActiveInfoScRsp GetDailyActiveInfo()
    {
        var dbData = Data;
        CheckAndResetDaily();

        var rsp = new GetDailyActiveInfoScRsp
        {
            Retcode = 0,
            IIMJCLBOPNC = dbData.DailyActivePoint,
        };

        foreach (var info in dbData.TodayQuests.Values)
        {
            rsp.IHOELLGBBKN.Add(info.ToProto((uint)Player.Data.WorldLevel));
        }

        return rsp;
    }

    private void CheckAndResetDaily()
    {
        uint currentDay = (uint)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 86400);

        if (Data.LastRefreshDay != currentDay || Data.TodayQuests.Count == 0)
        {
            Log.Info($"[日常实训] 玩家 {Player.Uid} 触发跨天重置或初始化。"); // 现在没问题了

            Data.DailyActivePoint = 0;
            Data.TakenRewardList.Clear();
            Data.TodayQuests.Clear();

            uint[] hardcodedIds = { 2100003, 2100131, 2100105, 2100101, 2100102 };
            foreach (var id in hardcodedIds)
            {
                Data.TodayQuests[id] = new DailyQuestInfo 
                { 
                    QuestId = id, 
                    Progress = 0, 
                    IsFinished = false 
                };
            }

            Data.LastRefreshDay = currentDay;
            DatabaseHelper.ToSaveUidList.Add(Player.Uid);
        }
    }

    public async ValueTask SyncDailyQuestsStatus()
    {
        var syncList = new List<QuestInfo>();
        foreach (var qId in Data.TodayQuests.Keys)
        {
            syncList.Add(new QuestInfo
            {
                QuestId = (int)qId,
                QuestStatus = QuestStatus.QuestDoing,
                Progress = 0
            });
        }
        // 修复报错 4：确保引用了 PlayerSync 命名空间
        await Player.SendPacket(new PacketPlayerSyncScNotify(syncList));
    }
}
