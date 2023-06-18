using AmongUs.GameOptions;
using System.Collections.Generic;

using TOHE.Roles.Core;

namespace TOHE.Roles.Crewmate;
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
    public override (byte? votedForId, int? numVotes, bool doVote) OnVote(byte voterId, byte sourceVotedForId)
    {
        var (votedForId, numVotes, doVote) = base.OnVote(voterId, sourceVotedForId);
        var baseVote = (votedForId, numVotes, doVote);
        if (voterId != Player.PlayerId) return baseVote;
        baseVote.doVote = OptionCanVote.GetBool();
        return baseVote;
    }
}