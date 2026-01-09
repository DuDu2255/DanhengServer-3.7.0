using EggLink.DanhengServer.Proto;
using SqlSugar;

namespace EggLink.DanhengServer.Database.Quests;

[SugarTable("daily_active_data")]
public class DailyActiveData : BaseDatabaseDataHelper
{
    // Uid 已经由基类 BaseDatabaseDataHelper 提供，并自动作为主键
    
    // 核心：记录上次刷新时间（Unix天数戳），用于判定跨天重置
    public uint LastRefreshDay { get; set; } = 0;

    // 当前活跃度总分 (0-500)
    public uint DailyActivePoint { get; set; } = 0;

    // 已领取的奖励档位列表 [100, 200, 300, 400, 500]
    [SugarColumn(IsJson = true)]
    public List<uint> TakenRewardList { get; set; } = [];

    // 今日随机出的 5 个任务条目
    // Key: QuestId (子任务ID), Value: 进度信息
    [SugarColumn(IsJson = true, ColumnDataType = "MEDIUMTEXT")]
    public Dictionary<uint, DailyQuestInfo> TodayQuests { get; set; } = [];
}

public class DailyQuestInfo
{
    public uint QuestId { get; set; }
    public uint Progress { get; set; }
    public bool IsFinished { get; set; }

    // 适配你的 DailyActivityInfo.proto 混淆字段
    public DailyActivityInfo ToProto(uint worldLevel)
    {
        return new DailyActivityInfo
        {
            IIMJCLBOPNC = QuestId,       // 字段 1: QuestId
            Level = Progress,            // 字段 7: Progress
            WorldLevel = worldLevel,     // 字段 11: 均衡等级
            NOPMENIAFJM = IsFinished     // 字段 15: 是否完成
        };
    }
}
