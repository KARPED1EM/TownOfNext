namespace TONX.Roles.Core.Interfaces;

/// <summary>
/// 当被票出时可以带走一人的职业 - 猫又
/// </summary>
public interface INekomata
{
    /// <summary>
    /// 道連れが発動するかのチェック
    /// </summary>
    /// <param name="deathReason">猫又的死因</param>
    /// <returns>道連れを発生させるならtrue</returns>
    public bool DoRevenge(CustomDeathReason deathReason);
    /// <summary>
    /// 检查玩家是否被选择为陪葬目标
    /// </summary>
    /// <param name="player">判定するプレイヤー</param>
    /// <returns>如果玩家存在于陪葬名单内则为true</returns>
    public bool IsCandidate(PlayerControl player);
}