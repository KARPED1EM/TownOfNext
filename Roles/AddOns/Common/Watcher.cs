using System.Collections.Generic;
using UnityEngine;
using TOHE.Roles.Core;
using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;
public static class Watcher
{
    private static readonly int Id = 80300;
    private static Color RoleColor = Utils.GetRoleColor(CustomRoles.Watcher);
    public static string SubRoleMark = Utils.ColorString(RoleColor, "Ｗ");
    private static List<byte> playerIdList = new();

    public static void SetupCustomOption()
    {
        SetupAddonOptions(Id, TabGroup.Addons, CustomRoles.Watcher);
        AddOnsAssignData.Create(Id + 10, CustomRoles.Watcher, true, true, true);
    }
    public static void Init()
    {
        playerIdList = new();
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
    }
    public static bool IsEnable => playerIdList.Count > 0;
    public static bool IsThisRole(byte playerId) => playerIdList.Contains(playerId);

}