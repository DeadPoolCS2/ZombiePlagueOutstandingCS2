namespace HanZombiePlagueS2;

/// <summary>
/// 丧尸原始属性快照
/// </summary>
public class ZombiePropertySnapshot
{
    /// <summary>
    /// 丧尸名字
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// 丧尸最大血量
    /// </summary>
    public int Health { get; set; }
    /// <summary>
    /// 作为母体丧尸时最大血量
    /// </summary>
    public int MotherHealth { get; set; }
    /// <summary>
    /// 移动速度
    /// </summary>
    public float Speed { get; set; }
    /// <summary>
    /// 伤害加成
    /// </summary>
    public float Damage { get; set; }
    /// <summary>
    /// 重力值
    /// </summary>
    public float Gravity { get; set; }
    /// <summary>
    /// 是否开启了自动回血
    /// </summary>
    public bool EnableRegen { get; set; }
    /// <summary>
    /// 自动回血间隔时间
    /// </summary>
    public float HpRegenSec { get; set; }
    /// <summary>
    /// 自动回血每次恢复量
    /// </summary>
    public int HpRegenHp { get; set; }
    /// <summary>
    /// 使用的模型路径
    /// </summary>
    public string ModelPath { get; set; } = string.Empty;
}