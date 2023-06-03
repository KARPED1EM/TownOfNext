using AmongUs.GameOptions;
using System.Collections.Generic;

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
            8021618,
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