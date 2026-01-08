using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Send.Activity;

public class PacketGetLoginActivityScRsp : BasePacket
{
    // 构造函数，传入 Proto 数据，并指定对应的 CmdId
    public PacketGetLoginActivityScRsp(GetLoginActivityScRsp proto) 
        : base((ushort)CmdIds.GetLoginActivityScRsp) 
    {
        this.SetData(proto);
    }
}
