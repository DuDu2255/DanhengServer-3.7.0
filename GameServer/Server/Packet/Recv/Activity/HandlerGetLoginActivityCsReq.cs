using EggLink.DanhengServer.GameServer.Server.Packet.Send.Activity;
using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Recv.Activity;

[Opcode(CmdIds.GetLoginActivityCsReq)] // 指令 ID 通常为 2699
public class HandlerGetLoginActivityCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        // 即使 Req 是空的，也建议进行反序列化以保持协议栈一致性
        // var req = GetLoginActivityCsReq.Parser.ParseFrom(data);

        // 核心逻辑：发送包含玩家签到数据的 ScRsp
        await connection.SendPacket(new PacketGetLoginActivityScRsp(connection.Player!));
    }
}
