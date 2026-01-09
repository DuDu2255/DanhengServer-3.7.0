using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;
using EggLink.DanhengServer.GameServer.Server.Packet.Send.Friend;
using EggLink.DanhengServer.Util;
using Google.Protobuf; // 必须引用

namespace EggLink.DanhengServer.GameServer.Server.Packet.Recv.Friend;

[Opcode(CmdIds.GetFriendRecommendLineupCsReq)]
public class HandlerGetFriendRecommendLineupCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        uint challengeId = 0;
        uint requestType = 2; // 默认给 2 (全服推荐)，防止解析失败

        // --- 底层解析逻辑：提取 ChallengeId 和 Type ---
        try 
        {
            var input = new CodedInputStream(data);
            uint tag;
           while ((tag = input.ReadTag()) != 0)
            {
                int fieldNumber = WireFormat.GetTagFieldNumber(tag);
                
                // 根据日志反推：
                // 原来 fieldNumber 15/1/8 解析出了 1 -> 这才是真正的 Type
                // 原来 fieldNumber 2/13 解析出了 1701 -> 这才是真正的 ChallengeId (Key)

                if (fieldNumber == 2 || fieldNumber == 13) // 修改点：这两个 Tag 才是关卡 ID
                {
                    challengeId = input.ReadUInt32();
                }
                else if (fieldNumber == 15 || fieldNumber == 1 || fieldNumber == 8) // 这些是 Type
                {
                    requestType = input.ReadUInt32();
                }
                else 
                {
                    input.SkipLastField();
                }
            }
        }
        catch (Exception ex)
        {
            Logger.GetByClassName().Error($"原生解析异常: {ex.Message}");
        }

        Logger.GetByClassName().Info($"[战报请求] 解析成功 - ID: {challengeId}, Type: {requestType}, Uid: {connection.Player?.Uid}");

        if (connection.Player?.FriendManager == null) return;

        // --- 核心修正：传入两个参数 ---
        // 调用我们刚才改好的方法，传入解析出的 ID 和请求类型
        var rspData = connection.Player.FriendManager.GetGlobalRecommendLineup(challengeId, requestType);
        
        // 发送回包
        await connection.SendPacket(new PacketGetFriendRecommendLineupScRsp(rspData));
    }
}
