using System.Collections.Generic;
using System.Linq;
using TOHE.Roles.Core;
using UnityEngine;
using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;
public static class Brakar
{
    private static readonly int Id = 81000;
    private static Color RoleColor = Utils.GetRoleColor(CustomRoles.Brakar);
    private static List<byte> playerIdList = new();

    private static Dictionary<byte, byte> BrakarVotes = new();

    public static void SetupCustomOption()
    {
        SetupAddonOptions(Id, TabGroup.Addons, CustomRoles.Brakar);
        AddOnsAssignData.Create(Id + 10, CustomRoles.Brakar, true, true, true);
    }
    public static void Init()
    {
        playerIdList = new();
        BrakarVotes = new();
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
    }
    public static bool IsEnable => playerIdList.Count > 0;
    public static bool IsThisRole(byte playerId) => playerIdList.Contains(playerId);
    public static void OnVote(byte voter, byte target)
    {
        if (playerIdList.Contains(voter))
        {
            BrakarVotes.TryAdd(voter, target);
            BrakarVotes[voter] = target;
        }
    }
    public static bool ChooseExileTarget(byte[] mostVotedPlayers, out byte target)
    {
        target = byte.MaxValue;
        if (mostVotedPlayers.Count(BrakarVotes.ContainsValue) == 1)
        {
            target = mostVotedPlayers.Where(BrakarVotes.ContainsValue).FirstOrDefault();
            Logger.Info($"Brakar Override Tie => {Utils.GetPlayerById(target)?.GetNameWithRole()}", "Brakar");
            return true;
        }
        return false;
    }
    public static void OnMeetingStart()
    {
        BrakarVotes = new();
    }
}