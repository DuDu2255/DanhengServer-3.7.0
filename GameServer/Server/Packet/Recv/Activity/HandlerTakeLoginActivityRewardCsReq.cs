// 文件路径: GameServer/Server/Packet/Recv/Activity/HandlerTakeLoginActivityRewardCsReq.cs
using EggLink.DanhengServer.GameServer.Server;
using EggLink.DanhengServer.GameServer.Server.Packet.Send.Activity;
using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Recv.Activity;

[Opcode(CmdIds.TakeLoginActivityRewardCsReq)]
public class HandlerTakeLoginActivityRewardCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = TakeLoginActivityRewardCsReq.Parser.ParseFrom(data);
        var player = connection.Player;

        if (player?.ActivityManager == null) return;

        // 解包 ActivityManager 返回的 (items, panelId, retcode)
        var (rewardProto, panelId, retcode) = await player.ActivityManager.TakeLoginReward(req.Id, req.TakeDays);

        // 发送 Packet，带入动态 panelId
        await connection.SendPacket(new PacketTakeLoginActivityRewardScRsp(req.Id, req.TakeDays, retcode, rewardProto, panelId));
    }
}
