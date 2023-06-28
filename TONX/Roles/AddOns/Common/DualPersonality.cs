using System.Collections.Generic;
using TONX.Roles.Core;
using UnityEngine;
using static TONX.Options;

namespace TONX.Roles.AddOns.Common;
public static class DualPersonality
{
    private static readonly int Id = 81500;
    private static Color RoleColor = Utils.GetRoleColor(CustomRoles.DualPersonality);
    private static List<byte> playerIdList = new();

    public static void SetupCustomOption()
    {
        SetupAddonOptions(Id, TabGroup.Addons, CustomRoles.DualPersonality);
        AddOnsAssignData.Create(Id + 10, CustomRoles.DualPersonality, true, true, false);
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