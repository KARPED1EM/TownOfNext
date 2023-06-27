using System.Collections.Generic;
using TOHE.Roles.Core;
using UnityEngine;
using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;
public static class Ntr
{
    private static readonly int Id = 80600;
    private static Color RoleColor = Utils.GetRoleColor(CustomRoles.Ntr);
    private static List<byte> playerIdList = new();

    public static void SetupCustomOption()
    {
        SetupAddonOptions(Id, TabGroup.OtherRoles, CustomRoles.Ntr);
        AddOnsAssignData.Create(Id + 10, CustomRoles.Ntr, true, true, true);
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