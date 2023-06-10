using AmongUs.GameOptions;

using TOHE.Roles.Core;

namespace TOHE.Roles.Crewmate;
public sealed class Observer : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        new(
            typeof(Observer),
            player => new Observer(player),
            CustomRoles.Observer,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            22600,
            null,
            "ob",
            "#a8e0fa"
        );
    public Observer(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }
}