using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Send.Quest;

public class PacketGetDailyActiveInfoScRsp : BasePacket
{
    // 模仿你的风格，直接传入填好数据的 proto 对象
    public PacketGetDailyActiveInfoScRsp(GetDailyActiveInfoScRsp proto) : base(CmdIds.GetDailyActiveInfoScRsp)
    {
        // 这里的 proto 已经在 QuestManager 里填好了 ID 和分数
        SetData(proto);
    }
}
