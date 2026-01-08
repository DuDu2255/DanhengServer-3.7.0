using EggLink.DanhengServer.GameServer.Server.Packet.Send.Activity;
using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Recv.Activity;

[Opcode(CmdIds.TakeLoginActivityRewardCsReq)] // 指令 ID 通常为 2598
public class HandlerTakeLoginActivityRewardCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        // 1. 解析客户端传来的数据（包含活动 ID 和 领取的天数索引）
        var req = TakeLoginActivityRewardCsReq.Parser.ParseFrom(data);
        var player = connection.Player!;
        
        // 2. 逻辑处理：调用 ActivityManager 检查是否能领取，并下发奖励
        // 注意：你需要确保你的 ActivityManager 实现了这个领奖方法
        var rewardItems = player.ActivityManager.TakeLoginReward(req.Id, req.TakeDays, out uint retcode);

        // 3. 发送 ScRsp 回包
        // 将 retcode 和 获得的道具传给 Packet 构造函数
        await connection.SendPacket(new PacketTakeLoginActivityRewardScRsp(req, retcode, rewardItems));
    }
}
