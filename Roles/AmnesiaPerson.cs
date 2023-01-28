using System.Collections.Generic;

namespace TownOfHost
{
    public static class AmnesiaPerson
    {
        private static readonly int Id = 464645;
        public static List<byte> playerIdList = new();

        public static void Init()
        {
            playerIdList = new();
        }

        public static void OnReportDeadBody(PlayerControl player, GameData.PlayerInfo target) {
            if (player.GetCustomRole() != CustomRoles.AmnesiaPerson) {
                return;
            }
            if (target == null) {
                return;
            }
            player.RpcSetRole(target.GetCustomRole().IsImpostor() ? AmongUs.GameOptions.RoleTypes.Impostor : AmongUs.GameOptions.RoleTypes.Crewmate);
            player.RpcSetCustomRole(target.GetCustomRole());
            var role = player.GetCustomRole();
            if (role.IsImpostor() || role.IsNK() || role.IsNeutralKilling())
            {
                HudManager.Instance.KillButton.SetTarget(player);
            }
        }

        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
        }

        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.AmnesiaPerson);
        }
    }
}
