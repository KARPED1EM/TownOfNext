using AmongUs.GameOptions;

using TOHE.Roles.Core;
using TOHE.Roles.Core.Interfaces;

namespace TOHE.Roles.Neutral;

public sealed class Opportunist : RoleBase, IAdditionalWinner
{
    public static readonly SimpleRoleInfo RoleInfo =
       SimpleRoleInfo.Create(
            typeof(Opportunist),
            player => new Opportunist(player),
            CustomRoles.Opportunist,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Neutral,
            50100,
            null,
            "op|投機者|投机",
            "#00ff00"
        );
    public Opportunist(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    public bool CheckWin(out AdditionalWinners winnerType)
    {
        winnerType = AdditionalWinners.Opportunist;
        return Player.IsAlive();
    }
}
