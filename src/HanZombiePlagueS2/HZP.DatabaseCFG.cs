namespace HanZombiePlagueS2;

/// <summary>MySQL persistence settings for Ammo Packs.</summary>
public class HZPDatabaseCFG
{
    /// <summary>Set to true to enable MySQL AP persistence. When false, AP is in-memory only.</summary>
    public bool Enabled { get; set; } = false;

    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 3306;
    public string Database { get; set; } = "zombieplague";
    public string User { get; set; } = "root";
    public string Password { get; set; } = "";

    /// <summary>Table used to store per-player ammo pack balances.</summary>
    public string TableName { get; set; } = "hzp_ammopacks";
}
