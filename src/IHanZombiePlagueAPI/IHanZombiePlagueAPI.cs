using SwiftlyS2.Shared.Players;


namespace HanZombiePlagueS2;

/// <summary>
/// Han Zombie Plague API.
/// Han 僵尸瘟疫 API.
/// </summary>
public interface IHanZombiePlagueAPI
{
    /// <summary>
    /// Get GameStart.
    /// 获取游戏开始状态.
    /// </summary>
    bool GameStart { get; }

    /// <summary>
    /// Get player IsZombie?.
    /// 获取玩家是否是丧尸?.
    /// </summary>
    bool HZP_IsZombie(int playerId);

    /// <summary>
    /// Get player IsMotherZombie?.
    /// 获取玩家是否是母体丧尸?.
    /// </summary>
    bool HZP_IsMotherZombie(int playerId);

    /// <summary>
    /// Get player IsNemesis?.
    /// 获取玩家是否是复仇之神?.
    /// </summary>
    bool HZP_IsNemesis(int playerId);

    /// <summary>
    /// Get player IsAssassin?.
    /// 获取玩家是否是暗杀者丧尸?.
    /// </summary>
    bool HZP_IsAssassin(int playerId);

    /// <summary>
    /// Get player IsSurvivor?.
    /// 获取玩家是否是幸存者?.
    /// </summary>
    bool HZP_IsSurvivor(int playerId);

    /// <summary>
    /// Get player IsSniper?.
    /// 获取玩家是否是狙击手?.
    /// </summary>
    bool HZP_IsSniper(int playerId);

    /// <summary>
    /// Get player IsHero?.
    /// 获取玩家是否是英雄?.
    /// </summary>
    bool HZP_IsHero(int playerId);

    /// <summary>
    /// Get player HaveScbaSuit?.
    /// 获取玩家是否购买拥有防化服?.
    /// </summary>
    bool HZP_PlayerHaveScbaSuit(int playerId);

    /// <summary>
    /// Get player In GodState?.
    /// 获取玩家是否处于购买了无敌状态?.
    /// </summary>
    bool HZP_PlayerHaveGodState(int playerId);

    /// <summary>
    /// Get player In InfiniteAmmoState?.
    /// 获取玩家是否处于购买了无限子弹状态?.
    /// </summary>
    bool HZP_PlayerHaveInfiniteAmmoState(int playerId);

    /// <summary>
    /// .
    /// 获取当前回合模式名称.
    /// 可用于在各种模式内定义自己的逻辑
    /// </summary>
    string HZP_GetCurrentModeName();

    /// <summary>
    /// .
    /// 将目标人类设置为丧尸.
    /// 可用于道具T病毒试剂(非感染)
    /// </summary>
    void HZP_SetTargetZombie(IPlayer player);

    /// <summary>
    /// .
    /// 将目标人类设置为丧尸.
    /// (非感染直接设置包括特殊丧尸)
    /// </summary>
    void HZP_SetTargetHuman(IPlayer player);

    /// <summary>
    /// .
    /// 将目标人类感染为母体丧尸.
    /// 感染目标与寻找母体逻辑一致
    /// </summary>
    void HZP_InfectMotherZombie(IPlayer player);

    /// <summary>
    /// .
    /// 将目标人类感染为丧尸.
    /// 感染目标与玩家感染逻辑一致
    /// 是否忽略感染目标的防化服
    /// </summary>
    void HZP_InfectPlayer(IPlayer player, bool IgnoreScbaSuit);

    /// <summary>
    /// .
    /// 将目标人类感染为复仇之神.
    /// </summary>
    void HZP_SetTargetNemesis(IPlayer player);

    /// <summary>
    /// .
    /// 将目标人类感染为暗杀者.
    /// </summary>
    void HZP_SetTargetAssassin(IPlayer player);

    /// <summary>
    /// .
    /// 让某个丧尸服用T病毒血清(特殊丧尸无法改变).
    /// </summary>
    void HZP_SetTargetTVaccine(IPlayer player);

    /// <summary>
    /// .
    /// 将目标人类设置为狙击手.
    /// </summary>
    void HZP_SetTargetSniper(IPlayer player);

    /// <summary>
    /// .
    /// 将目标人类设置为幸存者.
    /// </summary>
    void HZP_SetTargetSurvivor(IPlayer player);

    /// <summary>
    /// .
    /// 将目标人类设置为英雄.
    /// </summary>
    void HZP_SetTargetHero(IPlayer player);

    /// <summary>
    /// .
    /// 给予丧尸T病毒炸弹.
    /// </summary>
    void HZP_GiveTVirusGrenade(IPlayer player);

    /// <summary>
    /// .
    /// 给予人类玩家防化服.
    /// </summary>
    void HZP_GiveScbaSuit(IPlayer player);

    /// <summary>
    /// .
    /// 给予无敌状态(自定义 x/秒).
    /// </summary>
    void HZP_GiveGodState(IPlayer player, float time);

    /// <summary>
    /// .
    /// 给予无限子弹状态(自定义 x/秒).
    /// </summary>
    void HZP_GiveInfiniteAmmo(IPlayer player, float time);

    /// <summary>
    /// .
    /// 给予人类血量(自定义 x血量).
    /// 无法超过配置内各个职业的最大血量.
    /// </summary>
    void HZP_HumanAddHealth(IPlayer player, int valve);

    /// <summary>
    /// .
    /// 获取丧尸Class(名称).
    /// 可用于制作技能组等
    /// </summary>
    string HZP_GetZombieClassname(IPlayer player);

    /// <summary>
    /// .
    /// 获取丧尸Class最大血量.
    /// bool original 来源.
    /// true => 最大血量来源于配置内值.
    /// false => 最大血量来源于pawn.MaxHealth.
    /// </summary>
    int HZP_GetZombieMaxHealth(IPlayer player, bool original);

    /// <summary>
    /// .
    /// 检查回合胜利条件
    /// 调用后立即检查
    /// 检查阵营人数
    /// 如果满足结束条件结束回合
    /// </summary>
    void HZP_CheckRoundWinConditions();

    /// <summary>
    /// .
    /// 强制设置人类胜利
    /// </summary>
    void HZP_SetHumanWin();

    /// <summary>
    /// .
    /// 强制设置丧尸胜利
    /// </summary>
    void HZP_SetZombieWin();

    /// <summary>
    /// .
    /// 设置玩家外发光
    /// 死亡后自动删除
    /// 填写RGBA值
    /// </summary>
    void HZP_SetPlayerGlow(IPlayer player, int R, int G, int B, int A);

    /// <summary>
    /// 删除玩家外发光
    /// </summary>
    void HZP_RemovePlayerGlow(IPlayer player);

    /// <summary>
    /// .
    /// 设置玩家fov
    /// 死亡后自动返回90
    /// 填写fov值
    /// </summary>
    void HZP_SetPlayerFov(IPlayer player, int fov);

    /// <summary>
    /// .
    /// 给予玩家燃烧高爆手雷
    /// </summary>
    void HZP_GiveFireGrenade(IPlayer player);

    /// <summary>
    /// .
    /// 给予玩家照明弹
    /// </summary>
    void HZP_GiveLightGrenade(IPlayer player);

    /// <summary>
    /// .
    /// 给予玩家冰冻弹
    /// </summary>
    void HZP_GiveFreezeGrenade(IPlayer player);

    /// <summary>
    /// .
    /// 给予玩家传送手雷
    /// </summary>
    void HZP_GiveTeleportGrenade(IPlayer player);

    /// <summary>
    /// .
    /// 给予玩家火焰弹
    /// </summary>
    void HZP_GiveIncGrenade(IPlayer player);

    /// <summary>
    /// 外部设置玩家偏好
    /// 用于数据库保存
    /// 使用自己的数据库获取偏好
    /// 同步到插件内部字典
    /// string null或者空为随机
    /// </summary>
    void HZP_SetExternalPreference(ulong steamId, string? className);

    /// <summary>
    /// 供外部插件调用：根据 SteamID 获取玩家预设的丧尸类名
    /// </summary>
    /// <param name="steamId">玩家 SteamID64</param>
    /// <returns>返回丧尸类名；如果玩家设为随机或无记录，则返回 null</returns>
    string? HZP_GetZombieNameBySteamid(ulong steamId);

    /// <summary>
    /// 根据名称获取僵尸的所有核心属性快照
    /// </summary>
    ZombiePropertySnapshot? HZP_GetZombieProperties(string zombieName);

    /// <summary>
    /// 广播游戏开始事件
    /// bool 游戏开始
    /// </summary>
    event Action<bool>? HZP_OnGameStart;

    /// <summary>
    /// 广播感染事件
    /// IPlayer 感染者
    /// IPlayer 被感染者
    /// bool 手雷感染?
    /// string 被感染的丧尸名字
    /// </summary>
    event Action<IPlayer, IPlayer, bool, string>? HZP_OnPlayerInfect;

    /// <summary>
    /// 广播母体丧尸选择事件
    /// IPlayer 母体玩家
    /// </summary>
    event Action<IPlayer>? HZP_OnMotherZombieSelected;

    /// <summary>
    /// 广播复仇之神选择事件
    /// IPlayer 复仇之神玩家
    /// </summary>
    event Action<IPlayer>? HZP_OnNemesisSelected;

    /// <summary>
    /// 广播暗杀者选择事件
    /// IPlayer 复仇之神玩家
    /// </summary>
    event Action<IPlayer>? HZP_OnAssassinSelected;

    /// <summary>
    /// 广播英雄选择事件
    /// IPlayer 英雄玩家
    /// </summary>
    event Action<IPlayer>? HZP_OnHeroSelected;

    /// <summary>
    /// 广播幸存者选择事件
    /// IPlayer 幸存者玩家
    /// </summary>
    event Action<IPlayer>? HZP_OnSurvivorSelected;

    /// <summary>
    /// 广播狙击手选择事件
    /// IPlayer 狙击手玩家
    /// </summary>
    event Action<IPlayer>? HZP_OnSniperSelected;

    /// <summary>
    /// 广播人类胜利
    /// bool true 人类胜利
    /// bool false 丧尸胜利
    /// </summary>
    event Action<bool>? HZP_OnHumanWin;

    /// <summary>
    /// 广播模式选择
    /// string 配置模式名称
    /// 用于在各种模式内定义自己的逻辑
    /// </summary>
    event Action<string>? HZP_OnGameModeSelect;

    /// <summary>
    /// 丧尸偏好选择操作广播
    /// 当玩家用菜单选择了一个丧尸偏好后
    /// 调用这个方法，触发事件
    /// 让外部插件知道玩家改了偏好，可以存数据库了
    /// ulong steamid
    /// string 丧尸名字 空和null 为随机
    /// </summary>
    event Action<ulong, string?>? HZP_OnPreferenceChanged;



}