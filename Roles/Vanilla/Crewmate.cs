using AmongUs.GameOptions;

using TOHE.Roles.Core;

namespace TOHE.Roles.Vanilla;

public sealed class Crewmate : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.CreateForVanilla(
            typeof(Crewmate),
            player => new Crewmate(player),
            RoleTypes.Crewmate
        );
    public Crewmate(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }
}