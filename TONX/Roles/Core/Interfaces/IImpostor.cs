namespace TONX.Roles.Core.Interfaces;

/// <summary>
/// 内鬼的接口<br/>
/// <see cref="IKiller"/>的继承
/// </summary>
public interface IImpostor : IKiller
{
    /// <summary>
    /// 是否可以成为绝境者
    /// </summary>
    public bool CanBeLastImpostor => true;

    /// <summary>
    /// 可以作为内鬼使用通风管
    /// </summary>
    public bool CanUseImpostorVentButton => true;
}
