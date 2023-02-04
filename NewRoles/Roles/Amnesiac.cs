using AmongUs.GameOptions;
using TownOfHost.Listener;

namespace TownOfHost.NewRoles.Roles;

public class Amnesiac : Role, IListener
{
    public Amnesiac() : base(1919810, CustomRoles.Amnesiac)
    {
        Group = TabGroup.NeutralRoles;
        Color = "#00b4eb";
        HasTask = false;
    }

    public void OnPlayerReportBody(PlayerControl reporter, GameData.PlayerInfo target)
    {
        if (reporter.GetCustomRole() != CustomRoles.Amnesiac) return;
        new LateTask(() =>
        {
            reporter.RpcSetRole(target.GetCustomRole().IsImpostor() ? RoleTypes.Impostor : RoleTypes.Crewmate);
            reporter.RpcSetCustomRole(target.GetCustomRole());
        }, 1, "Task for amnesiac");
    }
}
