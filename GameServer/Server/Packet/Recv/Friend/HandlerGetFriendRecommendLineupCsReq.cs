
using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;
using EggLink.DanhengServer.GameServer.Server.Packet.Send.Friend;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Recv.Friend;

[Opcode(CmdIds.GetFriendRecommendLineupCsReq)]
public class HandlerGetFriendRecommendLineupCsReq : Handler
{
   // 在 HandlerGetFriendRecommendLineupCsReq.cs 中
public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
{
    // 直接用响应包的 Parser 解析！
    var req = GetFriendRecommendLineupScRsp.Parser.ParseFrom(data);
    
    // 只要这一步能拿到 Key (也就是 ChallengeId)，后面就全通了
    var rspData = connection.Player!.FriendManager!.GetGlobalRecommendLineup(req.Key);
    
    await connection.SendPacket(new PacketGetFriendRecommendLineupScRsp(rspData));
}
}
