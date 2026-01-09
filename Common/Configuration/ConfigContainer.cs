namespace EggLink.DanhengServer.Configuration;

public class ConfigContainer
{
    public HttpServerConfig HttpServer { get; set; } = new();
    public KeyStoreConfig KeyStore { get; set; } = new();
    public GameServerConfig GameServer { get; set; } = new();
    public PathConfig Path { get; set; } = new();
    public DatabaseConfig Database { get; set; } = new();
    public ServerOption ServerOption { get; set; } = new();
    public MuipServerConfig MuipServer { get; set; } = new();
}

public class HttpServerConfig
{
    public string BindAddress { get; set; } = "0.0.0.0";
    public string PublicAddress { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 23300;
    public bool UseSSL { get; set; } = false;
    public bool UseFetchRemoteHotfix { get; set; } = false;

    public string GetDisplayAddress()
    {
        return (UseSSL ? "https" : "http") + "://" + PublicAddress + ":" + Port;
    }

    public string GetBindDisplayAddress()
    {
        return (UseSSL ? "https" : "http") + "://" + BindAddress + ":" + Port;
    }
}

public class KeyStoreConfig
{
    public string KeyStorePath { get; set; } = "certificate.p12";
    public string KeyStorePassword { get; set; } = "123456";
}

public class GameServerConfig
{
    public string BindAddress { get; set; } = "0.0.0.0";
    public string PublicAddress { get; set; } = "127.0.0.1";
    public uint Port { get; set; } = 23301;
    public string GameServerId { get; set; } = "dan_heng";
    public string GameServerName { get; set; } = "DanhengServer";
    public string GameServerDescription { get; set; } = "A re-implementation of StarRail server";
    public bool UsePacketEncryption { get; set; } = true;

    public string GetDisplayAddress()
    {
        return PublicAddress + ":" + Port;
    }
}

public class PathConfig
{
    public string ResourcePath { get; set; } = "Resources";
    public string ConfigPath { get; set; } = "Config";
    public string DatabasePath { get; set; } = "Config/Database";
    public string LogPath { get; set; } = "Logs";
    public string PluginPath { get; set; } = "Plugins";
}

public class DatabaseConfig
{
    public string DatabaseType { get; set; } = "sqlite";
    public string DatabaseName { get; set; } = "danheng.db";
    public string MySqlHost { get; set; } = "127.0.0.1";
    public int MySqlPort { get; set; } = 3306;
    public string MySqlUser { get; set; } = "root";
    public string MySqlPassword { get; set; } = "123456";
    public string MySqlDatabase { get; set; } = "danheng";
}

public class ServerOption
{
    public int StartTrailblazerLevel { get; set; } = 1;
    public bool AutoUpgradeWorldLevel { get; set; } = true;
    public bool EnableMission { get; set; } = true; // experimental
    public bool EnableQuest { get; set; } = true; // experimental
    public bool AutoLightSection { get; set; } = true;
    public string Language { get; set; } = "CHS";
    public string FallbackLanguage { get; set; } = "EN";
    public HashSet<string> DefaultPermissions { get; set; } = ["*"];
    public ServerAnnounce ServerAnnounce { get; set; } = new();
    public ServerProfile ServerProfile { get; set; } = new();
    public bool AutoCreateUser { get; set; } = true;
    public LogOption LogOption { get; set; } = new();
    public ServerConfig ServerConfig { get; set; } = new();
    public int FarmingDropRate { get; set; } = 1;
    public bool UseCache { get; set; } = false; // didnt recommend

    public int ValidFarmingDropRate()
    {
        return Math.Max(Math.Min(FarmingDropRate, 999), 1);
    }
}

public class ServerConfig
{
    public bool RunDispatch { get; set; } = true;
    public string FromDispatchBaseUrl { get; set; } = "";
    public bool RunGateway { get; set; } = true; // if run gateway, also run game server
    public List<ServerRegion> Regions { get; set; } = [];
}

public class ServerRegion
{
    public string GateWayAddress { get; set; } = "";
    public string GameServerName { get; set; } = "";
    public string GameServerId { get; set; } = "";
    public int EnvType { get; set; } = 21;
}

public class LogOption
{
#if DEBUG
    public bool EnableGamePacketLog { get; set; } = true;
#else
    public bool EnableGamePacketLog { get; set; } = false;
#endif
    public bool LogPacketToConsole { get; set; } = true;
    public bool DisableLogDetailPacket { get; set; } = false;
    public bool SavePersonalDebugFile { get; set; } = false;
}

public class ServerAnnounce
{
    public bool EnableAnnounce { get; set; } = true;
    public string AnnounceContent { get; set; } = "Welcome to danhengserver!";
}

public class ServerProfile
{
    public string Name { get; set; } = "StopWuyu";
    public int Uid { get; set; } = 5201314;
    public string Signature { get; set; } = "Type /help for a list of commands";
    public int Level { get; set; } = 70;
    public int HeadIcon { get; set; } = 200105;
    public int ChatBubbleId { get; set; } = 220001;
    public int PersonalCardId { get; set; } = 253001;

    public List<ServerAssistInfo> AssistInfo { get; set; } =
    [
        new() { AvatarId = 1213, Level = 80 }
    ];
}

public class ServerAssistInfo
{
    public int AvatarId { get; set; }
    public int Level { get; set; }
    public int SkinId { get; set; }
}

public class MuipServerConfig
{
    public string AdminKey { get; set; } = "None";
}
