using System.Globalization;
using EggLink.DanhengServer.Enums.Task;

namespace EggLink.DanhengServer.Util;

public static class UtilTools
{
    public static string GetCurrentLanguage()
    {
        var uiCulture = CultureInfo.CurrentUICulture;
        return uiCulture.Name switch
        {
            "zh-CN" => "CHS",
            "zh-TW" => "CHT",
            "ja-JP" => "JP",
            _ => "EN"
        };
    }

    public static bool CheckAnd(List<Func<bool>> actions, bool defaultValue = true)
    {
        if (actions.Count == 0) return defaultValue;

        foreach (var action in actions)
            try
            {
                var returnValue = action.Invoke();
                if (!returnValue) return false;
            }
            catch
            {
                // ignored
            }

        return true;
    }

    public static bool CheckOr(List<Func<bool>> actions, bool defaultValue = false)
    {
        if (actions.Count == 0) return defaultValue;
        foreach (var action in actions)
            try
            {
                var returnValue = action.Invoke();
                if (returnValue) return true;
            }
            catch
            {
                // ignored
            }

        return false;
    }
    // 添加到 UtilTools 类内部
    public static bool IsSameDaily(long lastTime, long nowTime)
    {
        // 游戏通常以凌晨 4 点作为跨天线 (14400秒)
        var lastDate = DateTimeOffset.FromUnixTimeSeconds(lastTime - 14400).ToLocalTime().Date;
        var nowDate = DateTimeOffset.FromUnixTimeSeconds(nowTime - 14400).ToLocalTime().Date;
        return lastDate == nowDate;
    }
    public static bool CompareNumberByOperationEnum(int left, int right, CompareTypeEnum operation)
    {
        return operation switch
        {
            CompareTypeEnum.Greater => left > right,
            CompareTypeEnum.GreaterEqual => left >= right,
            CompareTypeEnum.NotEqual => left != right,
            CompareTypeEnum.Equal => left == right,
            CompareTypeEnum.LessEqual => left <= right,
            CompareTypeEnum.Less => left < right,
            _ => false
        };
    }
}
