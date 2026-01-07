using EggLink.DanhengServer.Proto;
using EggLink.DanhengServer.Kcp;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Send.Friend;

public class PacketGetFriendRecommendLineupScRsp : BasePacket
{
    // 构造函数：传入从数据库查出来的列表
    public PacketGetFriendRecommendLineupScRsp(GetFriendRecommendLineupScRsp rsp) 
        : base(CmdIds.GetFriendRecommendLineupScRsp)
    {
        SetData(rsp);
    }
}
