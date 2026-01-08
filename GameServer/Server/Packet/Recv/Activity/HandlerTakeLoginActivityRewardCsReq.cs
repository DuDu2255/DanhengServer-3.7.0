using EggLink.DanhengServer.GameServer.Server;
using EggLink.DanhengServer.GameServer.Server.Packet.Send.Activity;
using EggLink.DanhengServer.Kcp; // Opcode 所在的命名空间
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Recv.Activity;

[Opcode(CmdIds.TakeLoginActivityRewardCsReq)]
public class HandlerTakeLoginActivityRewardCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = TakeLoginActivityRewardCsReq.Parser.ParseFrom(data);
        var player = connection.Player;

        // 判空保护，消除警告
        if (player?.ActivityManager == null) return;

        // 解包异步元组 (items, panelId, retcode)
        var (rewardProto, panelId, retcode) = await player.ActivityManager.TakeLoginReward(req.Id, req.TakeDays);

        // 发送回包
        await connection.SendPacket(new PacketTakeLoginActivityRewardScRsp(req.Id, req.TakeDays, retcode, rewardProto, panelId));
    }
}
