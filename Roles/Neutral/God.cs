using AmongUs.GameOptions;
using System.Collections.Generic;
using UnityEngine;

using TOHE.Roles.Core.Interfaces;
using TOHE.Roles.Core;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;
public sealed class God : RoleBase, IOverrideWinner
{
    public static readonly SimpleRoleInfo RoleInfo =
        new(
            typeof(God),
            player => new God(player),
            CustomRoles.God,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Neutral,
            50300,
            SetupOptionItem,
            "go",
            "#f96464",
            experimental: true
        );
    public God(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False
    )
    { }

    private static OptionItem OptionNotifyGodAlive;
    enum OptionName
    {
        NotifyGodAlive
    }

    private static void SetupOptionItem()
    {
        OptionNotifyGodAlive = BooleanOptionItem.Create(RoleInfo, 10, OptionName.NotifyGodAlive, true, false);
    }
    public override void OverrideRoleNameAsSeer(PlayerControl seen, ref bool enabled, ref Color roleColor, ref string roleText)
    {
        enabled = true;
    }
    public override void NotifyOnMeetingStart(ref List<(string, byte, string)> msgToSend)
    {
        if (OptionNotifyGodAlive.GetBool())
            msgToSend.Add((GetString("GodNoticeAlive"), 255, Utils.ColorString(RoleInfo.RoleColor, GetString("GodAliveTitle"))));
    }
    public void CheckWin(ref CustomWinner WinnerTeam, ref HashSet<byte> WinnerIds)
    {
        if (Player.IsAlive() && WinnerTeam != CustomWinner.God)
        {
            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.God);
            CustomWinnerHolder.WinnerRoles.Add(CustomRoles.God);
        }
    }
}