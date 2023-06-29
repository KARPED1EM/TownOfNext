﻿using AmongUs.GameOptions;

using TONX.Roles.Core;

namespace TONX.Roles.Crewmate;
public sealed class Glitch : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Glitch),
            player => new Glitch(player),
            CustomRoles.Glitch,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            23000,
            SetupOptionItem,
            "gl|活死",
            "#dcdcdc"
        );
    public Glitch(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    static OptionItem OptionCanVote;
    enum OptionName
    {
        GlitchCanVote,
    }

    private static void SetupOptionItem()
    {
        OptionCanVote = BooleanOptionItem.Create(RoleInfo, 10, OptionName.GlitchCanVote, true, false);
    }
    public override bool OnVote(byte voterId, byte sourceVotedForId, ref byte roleVoteFor, ref int roleNumVotes, ref bool clearVote)
    {
        if (voterId != Player.PlayerId || OptionCanVote.GetBool()) return true;
        roleNumVotes = 0;
        return true;
    }
}