using EggLink.DanhengServer.Data;
using EggLink.DanhengServer.Database.Activity;
using EggLink.DanhengServer.GameServer.Server.Packet.Send.Activity;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Game.Activity.Activities;

public class TrialActivityInstance : BaseActivityInstance
{
    public TrialActivityInstance(ActivityManager manager) : base(manager)
    {
        Data = ActivityManager.Data.TrialActivityData;
    }

    public TrialActivityData Data { get; set; }

    public async ValueTask StartActivity(int stageId)
    {
        var player = ActivityManager.Player;

        // --- 最小化修复：记录进入前的位置 ---
        // 即使不新增数据库字段，也可以直接利用 player.Data 现有的记录
        // 如果想更稳妥，建议在数据库 TrialActivityData 增加 PrePlaneId 等字段

        await player.LineupManager!.DestroyExtraLineup(ExtraLineupType.LineupStageTrial);

        GameData.AvatarDemoConfigData.TryGetValue(stageId, out var excel);
        if (excel != null)
        {
            Data.CurTrialStageId = stageId;
            player.LineupManager.SetExtraLineup(ExtraLineupType.LineupStageTrial, excel.TrialAvatarList.ToList(), true);
            await player.EnterScene(excel.MapEntranceID, 0, true);
        }

        await player.SendPacket(new PacketStartTrialActivityScRsp((uint)stageId));
    }

    public async ValueTask EndActivity(TrialActivityStatus status = TrialActivityStatus.None)
    {
        var player = ActivityManager.Player!;

        // 1. 移除试用阵容，回滚到正常队伍
        await player.LineupManager!.DestroyExtraLineup(ExtraLineupType.LineupStageTrial);
        player.LineupManager!.LineupData.CurExtraLineup = -1;

        // 2. --- 修复传送 BUG：原地返回 ---
        // 如果玩家 Data 中记录的场景不是当前试用场景，则尝试原地 LoadScene
        // 否则兜底回星穹列车 (2000101)
        if (player.Data.PlaneId != 0 && player.Data.PlaneId != player.SceneInstance?.PlaneId)
        {
            await player.LoadScene(player.Data.PlaneId, player.Data.FloorId, player.Data.EntryId, player.Data.Pos, player.Data.Rot, true);
        }
        else
        {
            await player.EnterScene(2000101, 0, true);
        }

        // 3. 处理结算逻辑
        if (status == TrialActivityStatus.Finish)
        {
            // 防重复添加
            if (Data.Activities.All(x => x.StageId != Data.CurTrialStageId))
            {
                Data.Activities.Add(new TrialActivityResultData
                {
                    StageId = Data.CurTrialStageId
                });
            }

            // 发送完成通知弹出 UI
            await player.SendPacket(new PacketCurTrialActivityScNotify((uint)Data.CurTrialStageId, status));
            
            // --- 核心修复：立即同步全量数据，解决重登领奖 BUG ---
            await player.SendPacket(new PacketGetTrialActivityDataScRsp(player));
        }

        // 4. 重置状态
        Data.CurTrialStageId = 0;
    }
}
