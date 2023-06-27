using System.Collections.Generic;
using TOHE.Roles.AddOns.Common;
using TOHE.Roles.Core;
using UnityEngine;
using static TOHE.Options;

namespace TOHE.Roles.AddOns.Impostor;
public static class TicketsStealer
{
    private static readonly int Id = 81900;
    private static Color RoleColor = Utils.GetRoleColor(CustomRoles.TicketsStealer);
    private static List<byte> playerIdList = new();

    public static OptionItem OptionTicketsPerKill;

    public static void SetupCustomOption()
    {
        SetupAddonOptions(Id, TabGroup.Addons, CustomRoles.TicketsStealer);
        AddOnsAssignData.Create(Id + 10, CustomRoles.TicketsStealer, false, true, false);
        OptionTicketsPerKill = FloatOptionItem.Create(Id + 20, "TicketsPerKill", new(0.1f, 10f, 0.1f), 0.5f, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.TicketsStealer])
            .SetValueFormat(OptionFormat.Votes);
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
    public static bool OnVote(byte voterId, byte sourceVotedForId, ref byte roleVoteFor, ref int roleNumVotes, ref bool clearVote)
    {
        if (playerIdList.Contains(voterId))
        {
            roleNumVotes += (int)((PlayerState.GetByPlayerId(voterId)?.GetKillCount(true) ?? 0) * OptionTicketsPerKill.GetFloat());
            Logger.Info($"TicketsStealer Additional Votes: {roleNumVotes}", "TicketsStealer.OnVote");
        }
        return true;
    }
    public static string GetProgressText(byte playerId, bool comms = false)
    {
        if (!playerIdList.Contains(playerId)) return "";
        var votes = (int)((PlayerState.GetByPlayerId(playerId)?.GetKillCount(true) ?? 0) * OptionTicketsPerKill.GetFloat());
        return votes > 0 ? Utils.ColorString(RoleColor.ShadeColor(0.5f), $"+{votes}t") : "";
    }
}