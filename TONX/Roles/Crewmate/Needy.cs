using AmongUs.GameOptions;

using TONX.Roles.Core;

namespace TONX.Roles.Crewmate;
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
            "lg|擺爛人|摆烂",
            "#a4dffe"
        );
    public Needy(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }
}