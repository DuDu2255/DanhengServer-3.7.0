
using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;
using EggLink.DanhengServer.GameServer.Server.Packet.Send.Friend;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Recv.Friend;

[Opcode(CmdIds.GetFriendRecommendLineupCsReq)]
public class HandlerGetFriendRecommendLineupCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        // 解析请求，获取 ChallengeId (对应协议里的 Key)
        var req = GetFriendRecommendLineupCsReq.Parser.ParseFrom(data);
        
        // 生成全服战报列表
        var rspData = connection.Player!.FriendManager!.GetGlobalRecommendLineup(req.Key);
        
        // 发送回包给客户端
        await connection.SendPacket(new PacketGetFriendRecommendLineupScRsp(rspData));
    }
}
