using AmongUs.GameOptions;

using TONX.Roles.Core;
using TONX.Roles.Core.Interfaces;

namespace TONX.Roles.Neutral;

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
            "op|Ͷ�C��|Ͷ��",
            "#00ff00"
        );
    public Opportunist(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    public bool CheckWin(ref CustomRoles winnerRole)
    {
        return Player.IsAlive();
    }
}
