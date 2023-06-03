using AmongUs.GameOptions;
using System.Collections.Generic;

using TOHE.Roles.Core;

namespace TOHE.Roles.Crewmate;
public sealed class Glitch : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        new(
            typeof(Glitch),
            player => new Glitch(player),
            CustomRoles.Glitch,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            8023489,
            SetupOptionItem,
            "gl",
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
    public override bool OnVotingEnd(ref List<MeetingHud.VoterState> statesList, ref PlayerVoteArea pva) => OptionCanVote.GetBool();
    public override void OnCalculateVotes(ref PlayerVoteArea ps, ref int VoteNum)
    {
        if (!OptionCanVote.GetBool()) VoteNum = 0;
    }

}