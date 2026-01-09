using EggLink.DanhengServer.Data;
using EggLink.DanhengServer.Database;
using EggLink.DanhengServer.Database.Activity;
using EggLink.DanhengServer.GameServer.Game.Activity.Activities;
using EggLink.DanhengServer.GameServer.Game.Player;
using EggLink.DanhengServer.Proto;
using EggLink.DanhengServer.Util;

namespace EggLink.DanhengServer.GameServer.Game.Activity;

public class ActivityManager : BasePlayerManager
{
    public ActivityData Data { get; set; }
    public TrialActivityInstance? TrialActivityInstance { get; set; }

    public ActivityManager(PlayerInstance player) : base(player)
    {
        Data = DatabaseHelper.Instance!.GetInstanceOrCreateNew<ActivityData>(player.Uid);
        if (Data.TrialActivityData.CurTrialStageId != 0) 
            TrialActivityInstance = new TrialActivityInstance(this);
    }

    /// <summary>
    /// 【新增方法】：判定是否为签到类活动
    /// 规律：以 10018 为起点，每步进 5 为一个新活动 (10018, 10023, 10028...)
    /// </summary>
    private bool IsCheckInActivity(uint mainId)
    {
        if (mainId < 10018) return false;
        return (mainId - 10018) % 5 == 0;
    }

    /// <summary>
    /// 自动更新签到天数 (正式环境：已关闭 Debug 强制递增)
    /// </summary>
    public void UpdateLoginDays()
    {
        if (Data?.LoginActivityData == null) return;
        var loginData = Data.LoginActivityData;
        var now = Extensions.GetUnixSec();

        // 正式判定：首次进入 或 跨天判定 (凌晨4点逻辑)
        if (loginData.LastUpdateTick == 0 || !UtilTools.IsSameDaily(loginData.LastUpdateTick, now))
        {
            var schedules = GameData.ActivityConfig?.ScheduleData;
            if (schedules == null) return;

            // 筛选当前有效的活动
            var activeSchedules = schedules.Where(s => now >= s.BeginTime && now <= s.EndTime).ToList();

            bool updated = false;
            foreach (var schedule in activeSchedules)
            {
                // 统一取前 5 位主体 ID (去尾处理)
                uint mainId = (uint)schedule.ActivityId / 100;

                // 使用“+5规律”自动判定
                if (IsCheckInActivity(mainId))
                {
                    if (!loginData.LoginDays.ContainsKey(mainId))
                        loginData.LoginDays[mainId] = 1;
                    else if (loginData.LoginDays[mainId] < 7) 
                        loginData.LoginDays[mainId]++;
                    
                    updated = true;
                }
            }

            loginData.LastUpdateTick = now;

            // 标记异步保存
            if (!DatabaseHelper.ToSaveUidList.Contains(Player.Uid))
                DatabaseHelper.ToSaveUidList.Add(Player.Uid);
            
            Logger.GetByClassName().Info($"玩家 {Player.Uid} 签到检查完成。更新状态: {updated}");
        }
    }

    /// <summary>
    /// 领取奖励 (应用主体 ID 映射)
    /// </summary>
    public async Task<(ItemList items, uint panelId, uint retcode)> TakeLoginReward(uint activityId, uint takeDays)
    {
        var items = new ItemList();
        if (Data?.LoginActivityData == null) return (items, 10130, 1);

        var loginData = Data.LoginActivityData;
        
        // 统一处理为 5 位主体 ID
        uint mainId = activityId > 1000000 ? activityId / 100 : activityId;

        // 获取 PanelId 兼容
        var schedule = GameData.ActivityConfig?.ScheduleData?
            .FirstOrDefault(s => s.ActivityId == activityId || s.ActivityId / 100 == activityId);
        uint currentPanelId = (uint)(schedule?.PanelId ?? 10130);

        // 1. 进度检查
        if (!loginData.LoginDays.TryGetValue(mainId, out var currentDays) || takeDays > currentDays)
        {
            return (items, currentPanelId, 2602); // 2602: 进度不足
        }

        // 2. 重复领取检查
        if (!loginData.TakenRewards.ContainsKey(mainId))
            loginData.TakenRewards[mainId] = new List<uint>();

        if (loginData.TakenRewards[mainId].Contains(takeDays))
        {
            return (items, currentPanelId, 2002); // 2002: 已领取
        }

        // 3. 奖励确认 (102 = 星轨专票)
        uint count = takeDays switch { 1=>1, 2=>1, 3=>2, 4=>1, 5=>1, 6=>1, 7=>3, _=>0 };

        if (count > 0 && Player.InventoryManager != null)
        {
            items.ItemList_.Add(new Item { ItemId = 102, Num = count });
            await Player.InventoryManager.AddItem(102, (int)count, notify: true);
        }

        // 4. 更新记录并标记异步保存
        loginData.TakenRewards[mainId].Add(takeDays);
        if (!DatabaseHelper.ToSaveUidList.Contains(Player.Uid))
            DatabaseHelper.ToSaveUidList.Add(Player.Uid);

        return (items, currentPanelId, 0); 
    }

    /// <summary>
    /// 获取活动进度包
    /// </summary>
    public GetLoginActivityScRsp GetLoginInfo()
    {
        var rsp = new GetLoginActivityScRsp();
        if (Data?.LoginActivityData == null) return rsp;

        var loginProtoData = Data.LoginActivityData.ToProto();
        foreach (var proto in loginProtoData)
        {
            // 确保同步下发的 ID 能对应上 PanelId
            var config = GameData.ActivityConfig?.ScheduleData?
                .FirstOrDefault(s => s.ActivityId == proto.Id || s.ActivityId / 100 == proto.Id);
            if (config != null) proto.PanelId = (uint)config.PanelId;
        }
        rsp.LoginActivityList.AddRange(loginProtoData);
        return rsp;
    }

    public List<ActivityScheduleData> ToProto()
    {
        var proto = new List<ActivityScheduleData>();
        var schedules = GameData.ActivityConfig?.ScheduleData;
        if (schedules == null) return proto;

        foreach (var activity in schedules)
        {
            proto.Add(new ActivityScheduleData
            {
                ActivityId = (uint)activity.ActivityId,
                BeginTime = activity.BeginTime,
                EndTime = activity.EndTime,
                PanelId = (uint)activity.PanelId
            });
        }
        return proto;
    }
}
