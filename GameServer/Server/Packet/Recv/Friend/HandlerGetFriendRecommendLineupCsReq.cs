
using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;
using EggLink.DanhengServer.GameServer.Server.Packet.Send.Friend;
using EggLink.DanhengServer.Util; // 添加这一行
namespace EggLink.DanhengServer.GameServer.Server.Packet.Recv.Friend;


[Opcode(CmdIds.GetFriendRecommendLineupCsReq)]
public class HandlerGetFriendRecommendLineupCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = GetFriendRecommendLineupCsReq.Parser.ParseFrom(data);
        
        Logger.GetByClassName().Info($"[战报请求] 关卡ID: {req.Key}, Uid: {connection.Player?.Uid}");

        if (connection.Player?.FriendManager == null) return;
        var rspData = connection.Player.FriendManager.GetGlobalRecommendLineup(req.Key);
        
        await connection.SendPacket(new PacketGetFriendRecommendLineupScRsp(rspData));
    }
}
