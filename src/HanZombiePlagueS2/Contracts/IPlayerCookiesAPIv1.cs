// ─────────────────────────────────────────────────────────────────────────────
// Contract copy from https://github.com/DeadPoolCS2/Cookies
// (Cookies.Contract/src/IPlayerCookiesAPIv1.cs)
// ─────────────────────────────────────────────────────────────────────────────
using SwiftlyS2.Shared.Players;

namespace Cookies.Contract;

public interface IPlayerCookiesAPIv1
{
    public T? Get<T>(IPlayer player, string key);
    public T? Get<T>(long steamid, string key);

    public T? GetOrDefault<T>(IPlayer player, string key, T defaultValue);
    public T? GetOrDefault<T>(long steamid, string key, T defaultValue);

    public bool Has(IPlayer player, string key);
    public bool Has(long steamid, string key);

    public void Set<T>(IPlayer player, string key, T value);
    public void Set<T>(long steamid, string key, T value);

    public void Clear(IPlayer player);
    public void Clear(long steamid);

    public void Unset(IPlayer player, string key);
    public void Unset(long steamid, string key);

    public void Load(IPlayer player);

    public void Save(IPlayer player);
    public void Save(long steamid);
}
