using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Send.Activity;

public class PacketTakeLoginActivityRewardScRsp : BasePacket
{
    // 参数修正：将 List<ItemData> 改为 ItemList
    public PacketTakeLoginActivityRewardScRsp(uint activityId, uint takeDays, uint retcode, ItemList rewards) 
        : base((ushort)CmdIds.TakeLoginActivityRewardScRsp) 
    {
        var proto = new TakeLoginActivityRewardScRsp
        {
            Id = activityId,
            TakeDays = takeDays,
            Retcode = retcode,
            Reward = rewards, // 这里直接赋值，不需要再 Select 转换
            PanelId = 10130 
        };

        // 根据 3.7.0 的 BasePacket 定义使用 SetData
        SetData(proto); 
    }
}
