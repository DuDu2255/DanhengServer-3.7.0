using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;
using EggLink.DanhengServer.GameServer.Game.Player;
using Google.Protobuf;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Send.Activity;

// 在 3.7.0 版本中，基类是 BasePacket
public class PacketGetLoginActivityScRsp : BasePacket
{
    public PacketGetLoginActivityScRsp(PlayerInstance player) : base(CmdIds.GetLoginActivityScRsp)
    {
        var rsp = new GetLoginActivityScRsp
        {
            Retcode = 0
        };

       // 确保 ActivityManager 不为空再调用
      if (player.ActivityManager != null) {
       var loginDataList = player.ActivityManager.Data.LoginActivityData.ToProto();
       rsp.LoginActivityList.AddRange(loginDataList);
		}

        // 设置 BasePacket 的 Data 属性
        this.Data = rsp.ToByteArray();
    }
}
