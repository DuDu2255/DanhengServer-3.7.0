using EggLink.DanhengServer.Database;
using EggLink.DanhengServer.Database.Quests;
using EggLink.DanhengServer.GameServer.Game.Player;
using EggLink.DanhengServer.Proto;
using EggLink.DanhengServer.Util;

namespace EggLink.DanhengServer.GameServer.Game.Quest;

public class DailyActiveManager(PlayerInstance player) : BasePlayerManager(player)
{
    // 获取当前玩家的数据库记录
    public DailyActiveData Data => 
        DatabaseHelper.Instance!.GetInstanceOrCreateNew<DailyActiveData>(Player.Uid);

    /// <summary>
    /// 处理客户端 3398 请求：获取日常实训面板数据
    /// </summary>
    public GetDailyActiveInfoScRsp GetDailyActiveInfo()
    {
        var dbData = Data;

        // 1. 检查是否需要跨天重置
        CheckAndResetDaily();

        // 2. 组装返回包
        var rsp = new GetDailyActiveInfoScRsp
        {
            Retcode = 0,
            IIMJCLBOPNC = dbData.DailyActivePoint, // 8号字段: 总分数
        };

        // 3. 填充已领取的宝箱档位
        rsp.MBIBABKIANF.AddRange(dbData.TakenRewardList);

        // 4. 填充 5 个任务条目
        foreach (var info in dbData.TodayQuests.Values)
        {
            // 这里传入 Player.Data.WorldLevel，解决你之前的报错
            rsp.IHOELLGBBKN.Add(info.ToProto((uint)Player.Data.WorldLevel));
        }

        return rsp;
    }

    /// <summary>
    /// 跨天重置逻辑
    /// </summary>
    private void CheckAndResetDaily()
    {
        // 计算当前天数（以北京时间 4 点为刷新点可后续优化，先按 UTC 天数）
        uint currentDay = (uint)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 86400);

        if (Data.LastRefreshDay != currentDay || Data.TodayQuests.Count == 0)
        {
            Log.Info($"[日常实训] 玩家 {Player.Uid} 触发跨天重置或初始化。");

            Data.DailyActivePoint = 0;
            Data.TakenRewardList.Clear();
            Data.TodayQuests.Clear();

            // 硬编码：塞入你选好的那 5 个 ID
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

    /// <summary>
    /// 强制同步任务状态到左侧任务栏
    /// </summary>
    public async ValueTask SyncDailyQuestsStatus()
    {
        var syncList = new List<QuestInfo>();
        foreach (var qId in Data.TodayQuests.Keys)
        {
            syncList.Add(new QuestInfo
            {
                QuestId = (int)qId,
                QuestStatus = QuestStatus.QuestDoing, // 设为进行中
                Progress = 0
            });
        }
        // 调用你项目中通用的同步包
        await Player.SendPacket(new PacketPlayerSyncScNotify(syncList));
    }
}
