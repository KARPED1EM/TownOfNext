using System.Collections.Generic;
using AmongUs.GameOptions;
using TOHE.Roles.Core;

namespace TOHE.Roles.Crewmate;
public sealed class Mayor : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        new(
            typeof(Mayor),
            player => new Mayor(player),
            CustomRoles.Mayor,
            () => OptionHasPortableButton.GetBool() ? RoleTypes.Engineer : RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            20200,
            SetupOptionItem,
            "my",
            "#204d42",
            introSound: () => GetIntroSound(RoleTypes.Crewmate)
        );
    public Mayor(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        AdditionalVote = OptionAdditionalVote.GetInt();
        HasPortableButton = OptionHasPortableButton.GetBool();
        NumOfUseButton = OptionNumOfUseButton.GetInt();
        HideVote = OptionHideVote.GetBool();

        LeftButtonCount = NumOfUseButton;
    }

    private static OptionItem OptionAdditionalVote;
    private static OptionItem OptionHasPortableButton;
    private static OptionItem OptionNumOfUseButton;
    private static OptionItem OptionHideVote;
    enum OptionName
    {
        MayorAdditionalVote,
        MayorHasPortableButton,
        MayorNumOfUseButton,
        MayorHideVote,
    }
    public static int AdditionalVote;
    public static bool HasPortableButton;
    public static int NumOfUseButton;
    public static bool HideVote;

    public int LeftButtonCount;
    private static void SetupOptionItem()
    {
        OptionAdditionalVote = IntegerOptionItem.Create(RoleInfo, 10, OptionName.MayorAdditionalVote, new(1, 15, 1), 4, false)
            .SetValueFormat(OptionFormat.Votes);
        OptionHasPortableButton = BooleanOptionItem.Create(RoleInfo, 11, OptionName.MayorHasPortableButton, false, false);
        OptionNumOfUseButton = IntegerOptionItem.Create(RoleInfo, 12, OptionName.MayorNumOfUseButton, new(1, 99, 1), 3, false, OptionHasPortableButton)
            .SetValueFormat(OptionFormat.Times);
        OptionHideVote = BooleanOptionItem.Create(RoleInfo, 13, OptionName.MayorHideVote, false, false);
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.EngineerCooldown =
            LeftButtonCount <= 0
            ? 255f
            : opt.GetInt(Int32OptionNames.EmergencyCooldown);
        AURoleOptions.EngineerInVentMaxTime = 1;
    }
    public override bool OnEnterVent(PlayerPhysics physics, int ventId)
    {
        if (LeftButtonCount > 0)
        {
            var user = physics.myPlayer;
            physics.RpcBootFromVent(ventId);
            user?.ReportDeadBody(null);
            LeftButtonCount--;
        }

        return false;
    }
    public override bool OnVotingEnd(ref List<MeetingHud.VoterState> statesList, ref PlayerVoteArea pva)
    {
        if (HideVote) return true;
        for (var i = 0; i < AdditionalVote; i++)
        {
            statesList.Add(new MeetingHud.VoterState()
            {
                VoterId = pva.TargetPlayerId,
                VotedForId = pva.VotedFor
            });
        }
        return true;
    }
    public override void OnCalculateVotes(ref PlayerVoteArea ps, ref int VoteNum)
    {
        if (CustomRoleManager.GetByPlayerId(ps.TargetPlayerId) is Mayor) VoteNum += AdditionalVote;
    }
    public override void AfterMeetingTasks()
    {
        if (HasPortableButton)
            Player.RpcResetAbilityCooldown();
    }
}