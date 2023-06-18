using AmongUs.GameOptions;

using TOHE.Roles.Core;

namespace TOHE.Roles.Crewmate;
public sealed class Needy : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Needy),
            player => new Needy(player),
            CustomRoles.Needy,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            20200,
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