using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Send.Quest;

public class PacketDailyActiveInfoNotify : BasePacket
{
    public PacketDailyActiveInfoNotify(DailyActiveInfoNotify proto) : base(CmdIds.DailyActiveInfoNotify)
    {
        SetData(proto);
    }
}
