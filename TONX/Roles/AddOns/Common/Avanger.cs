using System.Collections.Generic;
using System.Linq;
using TONX.Roles.Core;
using UnityEngine;
using static TONX.Options;

namespace TONX.Roles.AddOns.Common;
public static class Avanger
{
    private static readonly int Id = 81400;
    private static Color RoleColor = Utils.GetRoleColor(CustomRoles.Avanger);
    private static List<byte> playerIdList = new();

    public static OptionItem OptionRevengeMode;
    public static OptionItem OptionRevengeNums;
    public static OptionItem OptionRevengeOnKilled;
    public static OptionItem OptionRevengeOnSuicide;
    public static readonly string[] revengeModes =
    {
        "AvangerMode.Killer",
        "AvangerMode.Random",
        "AvangerMode.Enimies",
        "AvangerMode.Teammates",
    };

    public static void SetupCustomOption()
    {
        SetupAddonOptions(Id, TabGroup.Addons, CustomRoles.Avanger);
        AddOnsAssignData.Create(Id + 10, CustomRoles.Avanger, true, true, true);
        OptionRevengeMode = StringOptionItem.Create(Id + 20, "AvangerRevengeMode", revengeModes, 1, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Avanger]);
        OptionRevengeNums = IntegerOptionItem.Create(Id + 21, "AvangerRevengeNums", new(1, 3, 1), 1, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Avanger])
            .SetValueFormat(OptionFormat.Players);
        OptionRevengeOnKilled = BooleanOptionItem.Create(Id + 22, "AvangerRevengeOnKilled", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Avanger]);
        OptionRevengeOnSuicide = BooleanOptionItem.Create(Id + 23, "AvangerRevengeOnSuicide", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Avanger]);
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
    public static void OnMurderPlayerOthers(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        if (!playerIdList.Contains(target.PlayerId)) return;
        if (info.IsSuicide
            ? !OptionRevengeOnSuicide.GetBool()
            : !OptionRevengeOnKilled.GetBool()
            ) return;

        List<PlayerControl> targets = new();
        if (OptionRevengeMode.GetInt() == 0)
        {
            targets.Add(killer);
        }
        else
        {
            List<PlayerControl> list = new();
            switch (OptionRevengeMode.GetInt())
            {
                case 1:
                    list = Main.AllAlivePlayerControls.ToList();
                    break;
                case 2:
                    list = Main.AllAlivePlayerControls.Where(p => p.GetCustomRole().GetCustomRoleTypes() != target.GetCustomRole().GetCustomRoleTypes()).ToList();
                    break;
                case 3:
                    list = Main.AllAlivePlayerControls.Where(p => p.GetCustomRole().GetCustomRoleTypes() == target.GetCustomRole().GetCustomRoleTypes()).ToList();
                    break;
            }
            list = list.Where(p => p != target).ToList();
            for (int i = 0; i < OptionRevengeNums.GetInt(); i++)
            {
                if (list.Count < 1) break;
                int index = IRandom.Instance.Next(0, list.Count);
                targets.Add(list[index]);
                list.RemoveAt(index);
            }
        }

        foreach (var pc in targets)
        {
            pc.SetRealKiller(target);
            pc.SetDeathReason(CustomDeathReason.Revenge);
            target.RpcMurderPlayerEx(pc);
            Logger.Info($"Avanger {target.GetNameWithRole()} revenged => {pc.GetNameWithRole()}", "Avanger.OnMurderPlayerOthers");
        }
    }
}