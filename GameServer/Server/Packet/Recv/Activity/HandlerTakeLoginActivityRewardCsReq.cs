using EggLink.DanhengServer.GameServer.Server;
using EggLink.DanhengServer.GameServer.Server.Packet; // 必须添加这个引用
using EggLink.DanhengServer.GameServer.Server.Packet.Send.Activity;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Recv.Activity;

internal class HandlerTakeLoginActivityRewardCsReq : BasePacketHandler
{
    public override uint CmdId => CmdIds.TakeLoginActivityRewardCsReq;

    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = TakeLoginActivityRewardCsReq.Parser.ParseFrom(data);
        var player = connection.Player;

        if (player?.ActivityManager == null) return;

        // 注意这里的解包顺序：items, panelId, retcode
        var (rewardProto, panelId, retcode) = await player.ActivityManager.TakeLoginReward(req.Id, req.TakeDays);

        // 发送 Packet
        await connection.SendPacket(new PacketTakeLoginActivityRewardScRsp(req.Id, req.TakeDays, retcode, rewardProto, panelId));
    }
}
