using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Send.Activity;

public class PacketTakeLoginActivityRewardScRsp : BasePacket
{
    public PacketTakeLoginActivityRewardScRsp(TakeLoginActivityRewardCsReq req, uint retcode, ItemList reward) 
        : base((ushort)CmdIds.TakeLoginActivityRewardScRsp)
    {
        var rsp = new TakeLoginActivityRewardScRsp
        {
            Retcode = retcode,
            Id = req.Id,
            TakeDays = req.TakeDays,
            Reward = reward,
            PanelId = 10130 // 默认面板ID
        };

        this.Data = rsp.ToByteArray();
    }
}
