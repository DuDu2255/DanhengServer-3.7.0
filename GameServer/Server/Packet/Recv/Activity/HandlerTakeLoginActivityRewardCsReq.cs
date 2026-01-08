using EggLink.DanhengServer.GameServer.Server.Packet.Send.Activity;
using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Recv.Activity;

[Opcode(CmdIds.TakeLoginActivityRewardCsReq)] // 指令 ID 通常为 2598
public class HandlerTakeLoginActivityRewardCsReq : Handler
{
   public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
{
    var req = TakeLoginActivityRewardCsReq.Parser.ParseFrom(data);
    var player = connection.Player;

    // 增加空检查消除警告
    if (player?.ActivityManager == null) return;

    var rewardProto = player.ActivityManager.TakeLoginReward(req.Id, req.TakeDays, out uint retcode);

    // 确保这里的参数顺序和类型与 Packet 类一致
    await connection.SendPacket(new PacketTakeLoginActivityRewardScRsp(req.Id, req.TakeDays, retcode, rewardProto));
}
}
