using EggLink.DanhengServer.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace EggLink.DanhengServer.Configuration;

public class HotfixContainer
{
    [JsonConverter(typeof(StringEnumConverter))]
    public BaseRegionEnum Region { get; set; } = BaseRegionEnum.None;

    public Dictionary<string, DownloadUrlConfig> HotfixData { get; set; } = [];
}

public class DownloadUrlConfig
{
    public string AssetBundleUrl { get; set; } = "asb/V3.7Live/output_12477160_5da2dc8d6e33_785951683a8623";
    public string ExAssetBundleUrl { get; set; } = "";
    public string ExResourceUrl { get; set; } = "design_data/V3.7Live/output_12611473_1ea87ad3f42a_748fabe9f3533c";
    public string LuaUrl { get; set; } = "lua/V3.7Live/output_12576450_666f169b3262_4d113c227ffbe2";
    public string IfixUrl { get; set; } = "ifix/V3.7Live/output_12623363_6a3715f341cc_9169fbfe61efd7";
}

public static class GateWayBaseUrl
{
    public const string CNBETA = "https://beta-release01-cn.bhsr.com/query_gateway";
    public const string CNPROD = "https://prod-gf-cn-dp01.bhsr.com/query_gateway";
    public const string OSBETA = "https://beta-release01-asia.starrails.com/query_gateway";
    public const string OSPROD = "https://prod-official-asia-dp01.starrails.com/query_gateway";
}

public static class BaseUrl
{
    public const string CN = "https://autopatchcn.bhsr.com/";
    public const string OS = "https://autopatchcn.bhsr.com/";
}
