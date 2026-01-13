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

    await player.LineupManager!.DestroyExtraLineup(ExtraLineupType.LineupStageTrial);
    player.LineupManager!.LineupData.CurExtraLineup = -1;

    // --- 修复警告：确保 pos 和 rot 不为 null ---
    if (player.Data.PlaneId != 0 && player.Data.PlaneId != player.SceneInstance?.PlaneId)
    {
        
        var safePos = player.Data.Pos ?? new Position();
        var safeRot = player.Data.Rot ?? new Position();

        await player.LoadScene(
            player.Data.PlaneId, 
            player.Data.FloorId, 
            player.Data.EntryId, 
            safePos, 
            safeRot, 
            true
        );
    }
    else
    {
        await player.EnterScene(2000101, 0, true);
    }

    if (status == TrialActivityStatus.Finish)
    {
        if (Data.Activities.All(x => x.StageId != Data.CurTrialStageId))
        {
            Data.Activities.Add(new TrialActivityResultData { StageId = Data.CurTrialStageId });
        }
        await player.SendPacket(new PacketCurTrialActivityScNotify((uint)Data.CurTrialStageId, status));
        await player.SendPacket(new PacketGetTrialActivityDataScRsp(player));
    }

    Data.CurTrialStageId = 0;
}
  
}
