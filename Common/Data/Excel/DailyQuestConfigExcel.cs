using EggLink.DanhengServer.Util;

namespace EggLink.DanhengServer.Data.Excel;

[ResourceEntity("DailyQuest.json")] // 对应你提供的 JSON 文件名
public class DailyQuestConfigExcel : ExcelResource
{
    public int DailyID { get; set; }
    public List<int> QuestList { get; set; } = [];
    public bool IsDelete { get; set; }
    public int MinLevel { get; set; }
    public int MaxLevel { get; set; }

    public override int GetId() => DailyID;

    public override void Loaded()
    {
        // 将数据加载到 GameData 的新字典中
        GameData.DailyQuestConfigData.TryAdd(DailyID, this);
    }
}
