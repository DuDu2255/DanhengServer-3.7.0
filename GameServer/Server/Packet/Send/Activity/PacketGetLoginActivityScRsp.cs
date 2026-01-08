public class PacketGetLoginActivityScRsp : NetPacket
{
    public PacketGetLoginActivityScRsp(Player player) : base(CmdIds.GetLoginActivityScRsp)
    {
        var rsp = new GetLoginActivityScRsp
        {
            Retcode = 0
        };

        // 从 ActivityData 转换为 Proto 列表 (使用你之前写的 ToProto 方法)
        rsp.LoginActivityList.AddRange(player.ActivityManager.Data.LoginActivityData.ToProto());

        this.Data = rsp.ToByteArray();
    }
}
