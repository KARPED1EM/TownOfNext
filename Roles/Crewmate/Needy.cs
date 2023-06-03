using AmongUs.GameOptions;

using TOHE.Roles.Core;

namespace TOHE.Roles.Crewmate;
public sealed class Needy : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        new(
            typeof(Needy),
            player => new Needy(player),
            CustomRoles.Needy,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            1020095,
            null,
            "lg",
            "#a4dffe"
        );
    public Needy(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }
}