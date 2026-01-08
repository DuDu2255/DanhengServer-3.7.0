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

    /// <summary>
    /// 自动更新签到天数：只要跨天（无论时间前进还是倒退）就增加进度
    /// </summary>
    public void UpdateLoginDays()
    {
        var loginData = Data.LoginActivityData;
        var now = Extensions.GetUnixSec();

        // 只要当前时间戳和上次记录的时间戳不在同一个游戏天（凌晨4点跨天）
        if (!UtilTools.IsSameDaily(loginData.LastUpdateTick, now))
        {
            // 1. 定义指定的签到活动 ID
            uint[] targetCheckInIds = { 1001801, 1002301, 1002801 };

            // 2. 从配置中筛选出“当前时间点”在有效期内的活动
            var activeSchedules = GameData.ActivityConfig.ScheduleData
                .Where(s => now >= long.Parse(s.BeginTime) && now <= long.Parse(s.EndTime))
                .ToList();

            bool updated = false;
            foreach (var schedule in activeSchedules)
            {
                // 3. 判断是否为指定的签到 ID
                if (targetCheckInIds.Contains((uint)schedule.ActivityId))
                {
                    uint id = (uint)schedule.ActivityId;
                    
                    if (!loginData.LoginDays.ContainsKey(id))
                    {
                        loginData.LoginDays[id] = 1; // 初始第一天
                    }
                    else if (loginData.LoginDays[id] < 7) // 巡星之礼通常上限 7 天
                    {
                        loginData.LoginDays[id]++;
                    }
                    updated = true;
                }
            }

            // 4. 只要跨天了，就同步最后检查的时间戳，防止同天内重复触发
            loginData.LastUpdateTick = now;
            
            // 只要发生了数据变动（无论是天数加了，还是时间记录点变了），就同步到数据库
            DatabaseHelper.SaveInstance(this.Player.Data);
            
            Logger.Info($"玩家 {Player.Uid} 触发签到跨天检查。当前时间: {now}, 状态: {(updated ? "进度已增加" : "当前无活跃的目标签到活动")}");
        }
    }

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
/// <summary>
    /// 领取签到奖励逻辑
    /// </summary>
    /// <returns>返回包含奖励列表、面板ID和错误码的元组</returns>
    public async Task<(ItemList items, uint panelId, uint retcode)> TakeLoginReward(uint activityId, uint takeDays)
    {
        var items = new ItemList();
        var loginData = Data.LoginActivityData;
        
        // 查找该活动对应的 PanelId，查不到则默认 10130
        var schedule = GameData.ActivityConfig.ScheduleData.FirstOrDefault(s => s.ActivityId == activityId);
        uint currentPanelId = (uint)(schedule?.PanelId ?? 10130);

        // 1. 校验：是否达到签到天数
        if (!loginData.LoginDays.ContainsKey(activityId) || takeDays > loginData.LoginDays[activityId])
        {
            return (items, currentPanelId, 2003); // 天数不足
        }

        // 2. 状态校验：是否已领取
        if (!loginData.TakenRewards.ContainsKey(activityId))
            loginData.TakenRewards[activityId] = new List<uint>();

        if (loginData.TakenRewards[activityId].Contains(takeDays))
        {
            return (items, currentPanelId, 2002); // 已领过
        }

        // 3. 定义奖励序列 (1, 1, 2, 1, 1, 1, 3)
        uint rewardItemId = 1101; // 星轨专票
        uint count = takeDays switch
        {
            1 => 1, 2 => 1, 3 => 2, 4 => 1, 5 => 1, 6 => 1, 7 => 3,
            _ => 0
        };

        // 4. 执行发放
        if (count > 0 && Player.InventoryManager != null)
        {
            items.ItemList_.Add(new Item { ItemId = rewardItemId, Num = count });
            await Player.InventoryManager.AddItem((int)rewardItemId, (int)count, notify: true);
        }

        // 5. 更新并保存
        loginData.TakenRewards[activityId].Add(takeDays);
        DatabaseHelper.SaveInstance(this.Player.Data);

        return (items, currentPanelId, 0); // 成功
    }
  
}
