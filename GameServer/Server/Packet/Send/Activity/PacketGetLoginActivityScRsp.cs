using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;
using Google.Protobuf;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Send.Activity;

public class PacketGetLoginActivityScRsp : NetPacket
{
    // 构造函数，传入 Player 对象以获取其活动进度
    public PacketGetLoginActivityScRsp(PlayerInstance player) : base(CmdIds.GetLoginActivityScRsp)
    {
        var rsp = new GetLoginActivityScRsp
        {
            Retcode = 0
        };

        // 调用你之前在 ActivityData.cs 中定义的 ToProto 方法
        // 这一步会将数据库中的登录天数和已领奖励列表填充进响应包
        var loginDataList = player.ActivityManager.Data.LoginActivityData.ToProto();
        rsp.LoginActivityList.AddRange(loginDataList);

        // 将 Protobuf 对象序列化为字节数组存入 NetPacket 的 Data
        this.Data = rsp.ToByteArray();
    }
}
