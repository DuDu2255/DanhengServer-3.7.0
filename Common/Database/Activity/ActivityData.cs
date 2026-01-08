using EggLink.DanhengServer.Proto;
using SqlSugar;

namespace EggLink.DanhengServer.Database.Activity;

[SugarTable("Activity")]
public class ActivityData : BaseDatabaseDataHelper
{
    [SugarColumn(IsJson = true)] public TrialActivityData TrialActivityData { get; set; } = new();
    // 必须添加这一行，否则 PacketGetLoginActivityScRsp 会报错
    [SugarColumn(IsJson = true)] public LoginActivityData LoginActivityData { get; set; } = new();
}
public class LoginActivityData
{
    public Dictionary<uint, List<uint>> TakenRewards { get; set; } = new();
    public Dictionary<uint, uint> LoginDays { get; set; } = new();
    public long LastUpdateTick { get; set; }
    public List<EggLink.DanhengServer.Proto.LoginActivityData> ToProto()
{
    var protoList = new List<EggLink.DanhengServer.Proto.LoginActivityData>();

    foreach (var kv in LoginDays)
    {
        var activityId = kv.Key;
        var days = kv.Value;

        var protoData = new EggLink.DanhengServer.Proto.LoginActivityData
        {
            Id = activityId,
            LoginDays = days,
            // 注意：这里不要写 MLGBIGIECCO = ...
        };

        // 必须使用 AddRange 将数据库里的 List 导入到 Proto 的 RepeatedField 中
        if (TakenRewards.TryGetValue(activityId, out var takenList))
        {
            // 确保 takenList 是 List<uint>，然后加入到混淆名的字段中
            protoData.MLGBIGIECCO.AddRange(takenList); 
        }

        protoList.Add(protoData);
    }

    return protoList;
}
    
}
public class TrialActivityData
{
    public List<TrialActivityResultData> Activities { get; set; } = new();
    public int CurTrialStageId { get; set; } = 0;

    public List<TrialActivityInfo> ToProto()
    {
        var proto = new List<TrialActivityInfo>();

        foreach (var activity in Activities)
            proto.Add(new TrialActivityInfo
            {
                StageId = (uint)activity.StageId,
                TakenReward = activity.TakenReward
            });

        return proto;
    }
}

public class TrialActivityResultData
{
    public int StageId { get; set; } = 0;
    public bool TakenReward { get; set; } = false;
}
