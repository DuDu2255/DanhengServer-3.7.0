using EggLink.DanhengServer.Database.Inventory;
using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Send.Activity;

public class PacketTakeLoginActivityRewardScRsp : BasePacket
{
    // 参数：活动ID，领取的天数，返回码，奖励列表
    public PacketTakeLoginActivityRewardScRsp(uint activityId, uint takeDays, uint retcode, List<ItemData> rewards) 
        : base(CmdIds.TakeLoginActivityRewardScRsp) // 必须使用签到奖励的ID
    {
        var proto = new TakeLoginActivityRewardScRsp
        {
            Id = activityId,
            TakeDays = takeDays,
            Retcode = retcode,
            Reward = new ItemList(),
            PanelId = 10130 // 默认签到面板ID，也可以动态传入
        };

        // 填充奖励物品
        if (rewards != null && rewards.Count > 0)
        {
            proto.Reward.ItemList_.Add(rewards.Select(x => x.ToProto()).ToArray());
        }

        // 3.7.0版本通常使用 SetData 或 this.Data = ...
        SetData(proto); 
    }
}
