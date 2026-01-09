using EggLink.DanhengServer.GameServer.Server.Packet.Send.Quest; 
using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Recv.Quest;

[Opcode(CmdIds.GetDailyActiveInfoCsReq)] // 指令号 3398
public class HandlerGetDailyActiveInfoCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        // 1. 解析请求（虽然这个包通常没内容，但遵循你的代码风格）
        var req = GetDailyActiveInfoCsReq.Parser.ParseFrom(data);
        
        // 2. 调用 QuestManager 处理日常数据的逻辑（包含你那 5 个硬编码 ID）
        var rsp = connection.Player!.QuestManager!.GetDailyActiveInfo();
        
        // 3. 回送 Packet（PacketGetDailyActiveInfoScRsp 需要你新建，见下文）
        await connection.SendPacket(new PacketGetDailyActiveInfoScRsp(rsp));
        
        // 4. 打印调试日志，确认消息已发出
        // Log.Info($"[日常实训] 玩家 {connection.Player.Uid} 打开了实训界面，已下发硬编码任务列表。");
    }
}
