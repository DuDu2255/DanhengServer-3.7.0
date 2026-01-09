using EggLink.DanhengServer.Util;

namespace EggLink.DanhengServer.Data.Excel;

// 服务器会自动通过反射找到这个类并加载对应的 JSON
[ResourceEntity("DailyQuest.json")] 
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
        // 核心：加载时自动注入 GameData
        GameData.DailyQuestConfigData.TryAdd(DailyID, this);
    }
}
