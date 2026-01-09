using EggLink.DanhengServer.Database;
using EggLink.DanhengServer.Database.Quests;
using EggLink.DanhengServer.GameServer.Game.Player;
using EggLink.DanhengServer.GameServer.Server.Packet.Send.PlayerSync; 
using EggLink.DanhengServer.Proto;
using EggLink.DanhengServer.Util; 
using EggLink.DanhengServer.GameServer.Server.Packet.Send.Quest;
using EggLink.DanhengServer.Data; // 添加这一行来修复 GameData 找不到的问题
namespace EggLink.DanhengServer.GameServer.Game.Quest;

public class DailyActiveManager(PlayerInstance player) : BasePlayerManager(player)
{
    // 获取 Logger 实例以修复报错 3
    private static readonly Logger Log = Logger.GetByClassName();

    public DailyActiveData Data => 
        DatabaseHelper.Instance!.GetInstanceOrCreateNew<DailyActiveData>(Player.Uid);
    /// <summary>
/// 主动同步当前的活跃度分数给客户端 (CmdId: 3327)
/// </summary>
public async ValueTask SyncDailyActiveNotify()
{
    var notify = new DailyActiveInfoNotify
    {
        IIMJCLBOPNC = Data.DailyActivePoint // 对应协议中的分数进度字段
    };
    
    // 这里需要确保你已经写了 PacketDailyActiveInfoNotify 类
    await Player.SendPacket(new PacketDailyActiveInfoNotify(notify));
}
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
    long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    // 使用你写的 UtilTools 进行跨天判定
    // 如果数据库里的上次刷新时间 Data.LastRefreshTime 和现在不是同一天，则重置
    if (!UtilTools.IsSameDaily(Data.LastRefreshTime, now) || Data.TodayQuests.Count == 0)
    {
        Log.Info($"[日常实训] 触发跨天更新。上一次刷新时间: {Data.LastRefreshTime}, 当前时间: {now}");

        // 1. 清理旧数据
        Data.DailyActivePoint = 0;
        Data.TakenRewardList.Clear();
        Data.TodayQuests.Clear();

        // 2. 动态筛选任务池
        var availablePool = GameData.DailyQuestConfigData.Values
            .Where(x => !x.IsDelete && 
                        Player.Data.Level >= x.MinLevel && 
                        Player.Data.Level <= x.MaxLevel)
            .ToList();

        if (availablePool.Count > 0)
        {
            // 3. 随机抽取 5 组任务
            var random = new Random();
            var selectedGroups = availablePool.OrderBy(x => random.Next()).Take(5).ToList();

            foreach (var group in selectedGroups)
            {
                foreach (var qId in group.QuestList)
                {
                    Data.TodayQuests[(uint)qId] = new DailyQuestInfo 
                    { 
                        QuestId = (uint)qId, 
                        Progress = 0, 
                        IsFinished = false 
                    };
                }
            }
        }

        // 4. 更新刷新时间并保存 UID
        Data.LastRefreshTime = now; // 注意：你数据库字段名建议改为 LastRefreshTime
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
