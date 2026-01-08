using EggLink.DanhengServer.GameServer.Server;
using EggLink.DanhengServer.GameServer.Server.Packet.Send.Activity;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Recv.Activity;

internal class HandlerTakeLoginActivityRewardCsReq : BasePacketHandler
{
    // 确保 CmdId 对应 2698 
    public override uint CmdId => CmdIds.TakeLoginActivityRewardCsReq;

    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        // 1. 解析客户端请求
        var req = TakeLoginActivityRewardCsReq.Parser.ParseFrom(data);
        var player = connection.Player;

        // 2. 消除警告：判空检查
        // 如果玩家对象或 Manager 未初始化，直接返回，确保后续 player.ActivityManager 不会报 null 警告
        if (player?.ActivityManager == null) 
        {
            return; 
        }

        // 3. 核心修改：使用 await 调用异步的领奖逻辑
        // 因为我们修改了 ActivityManager 里的 TakeLoginReward 为 async Task
        var rewardProto = await player.ActivityManager.TakeLoginReward(req.Id, req.TakeDays, out uint retcode);

        // 4. 发送回包
        // 这里的 Packet 构造函数应该接收 (uint, uint, uint, ItemList)
        await connection.SendPacket(new PacketTakeLoginActivityRewardScRsp(req.Id, req.TakeDays, retcode, rewardProto));
    }
}
