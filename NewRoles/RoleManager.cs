using System.Collections.Generic;
using TownOfHost.Listener;

namespace TownOfHost.NewRoles;

public class RoleManager
{
    private static readonly List<Role> Roles = new();

    public static void RegisterRole(Role role)
    {
        Roles.Add(role);
    }

    public static List<Role> GetRoles()
    {
        return Roles;
    }

    public static void RegisterRoleWithListener(object obj)
    {
        RegisterRole(obj as Role);
        ListenerManager.RegisterListener(obj as IListener);
    }
}