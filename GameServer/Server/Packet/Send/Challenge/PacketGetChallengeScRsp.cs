using EggLink.DanhengServer.Data;
using EggLink.DanhengServer.GameServer.Game.Player;
using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Send.Challenge;

public class PacketGetChallengeScRsp : BasePacket
{
    public PacketGetChallengeScRsp(PlayerInstance player) : base(CmdIds.GetChallengeScRsp)
    {
        var proto = new GetChallengeScRsp { Retcode = 0 };
        
        // 从数据库获取的玩家通关历史记录
        var historyMap = player.ChallengeManager?.ChallengeData.History;

        foreach (var challengeExcel in GameData.ChallengeConfigData.Values)
        {
            // 检查数据库里是否有这一关的通关记录
            bool hasHistory = historyMap?.ContainsKey(challengeExcel.ID) ?? false;

            // 解锁逻辑：
            // 1. 本关已经打过了
            // 2. 是该组的第一关 (PreChallengeMazeID == 0)
            // 3. 数据库记录里包含前置关卡的 ID (说明前一关打赢了)
            bool isUnlocked = hasHistory || 
                              challengeExcel.PreChallengeMazeID == 0 || 
                              (historyMap != null && historyMap.ContainsKey(challengeExcel.PreChallengeMazeID));

            // 只有解锁的关卡才发送给客户端，未解锁的关卡客户端不可见
            if (isUnlocked)
            {
                if (hasHistory)
                {
                    // 已通关：发送数据库里存的星级、分数等详细信息
                    proto.ChallengeList.Add(historyMap![challengeExcel.ID].ToProto());
                }
                else
                {
                    // 已解锁但未通关：发送一个基础的 Challenge 对象告知客户端有这一关
                    proto.ChallengeList.Add(new Proto.Challenge
                    {
                        ChallengeId = (uint)challengeExcel.ID
                    });
                }
            }
        }

        // 下发奖励领取状态（保持不变）
        foreach (var reward in player.ChallengeManager?.ChallengeData?.TakenRewards.Values.ToList() ?? [])
            proto.ChallengeGroupList.Add(reward.ToProto());

        SetData(proto);
    }
}
