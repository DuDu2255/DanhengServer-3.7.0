using EggLink.DanhengServer.Data;
using EggLink.DanhengServer.Database;
using EggLink.DanhengServer.Database.Activity;
using EggLink.DanhengServer.GameServer.Game.Activity.Activities;
using EggLink.DanhengServer.GameServer.Game.Player;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Game.Activity;

public class ActivityManager : BasePlayerManager
{
    public ActivityManager(PlayerInstance player) : base(player)
    {
        Data = DatabaseHelper.Instance!.GetInstanceOrCreateNew<ActivityData>(player.Uid);

        if (Data.TrialActivityData.CurTrialStageId != 0) TrialActivityInstance = new TrialActivityInstance(this);
    }

    #region Data

    public ActivityData Data { get; set; }

    #endregion

    #region Instance

    public TrialActivityInstance? TrialActivityInstance { get; set; }

    #endregion

    public List<ActivityScheduleData> ToProto()
    {
        var proto = new List<ActivityScheduleData>();

        foreach (var activity in GameData.ActivityConfig.ScheduleData)
            proto.Add(new ActivityScheduleData
            {
                ActivityId = (uint)activity.ActivityId,
                BeginTime = activity.BeginTime,
                EndTime = activity.EndTime,
                PanelId = (uint)activity.PanelId
            });

        return proto;
    }
    public ItemList TakeLoginReward(uint activityId, uint takeDays, out uint retcode)
{
    var items = new ItemList();
    var loginData = Data.LoginActivityData;

    // 逻辑校验
    if (!loginData.LoginDays.ContainsKey(activityId) || takeDays > loginData.LoginDays[activityId])
    {
        retcode = 2003; // 天数不足
        return items;
    }

    if (!loginData.TakenRewards.ContainsKey(activityId))
        loginData.TakenRewards[activityId] = new List<uint>();

    if (loginData.TakenRewards[activityId].Contains(takeDays))
    {
        retcode = 2002; // 已领过
        return items;
    }

    // TODO: 这里应该从配置表读取奖励，暂时写死做测试
    // 将原本的 Count 改为 Num
    items.ItemList_.Add(new Item { ItemId = 102, Num = 100 }); 

    // 更新数据库
    loginData.TakenRewards[activityId].Add(takeDays);
    this.Player.Save(); 

    retcode = 0;
    return items;
}
    
}
