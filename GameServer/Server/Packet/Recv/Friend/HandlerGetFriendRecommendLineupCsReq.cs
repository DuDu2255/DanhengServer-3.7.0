[Opcode(CmdIds.GetFriendRecommendLineupCsReq)]
public class HandlerGetFriendRecommendLineupCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = GetFriendRecommendLineupCsReq.Parser.ParseFrom(data);
        
        // 调用逻辑获取全服阵容
        var rspData = connection.Player!.FriendManager!.GetGlobalRecommendLineup(req.Key);
        
        await connection.SendPacket(new PacketGetFriendRecommendLineupScRsp(rspData));
    }
}
