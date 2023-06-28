using AmongUs.GameOptions;
using TONX.Modules;
using TONX.Roles.Core;

namespace TONX.Roles.Crewmate;
public sealed class Dictator : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Dictator),
            player => new Dictator(player),
            CustomRoles.Dictator,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            21200,
            null,
            "dic|ªš²ÃÕß|¶À²Ã",
            "#df9b00"
        );
    public Dictator(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    public override void OnStartMeeting() => LastVoted = byte.MaxValue;
    private byte LastVoted;
    public override bool OnVote(byte voterId, byte sourceVotedForId, ref byte roleVoteFor, ref int roleNumVotes, ref bool clearVote)
    {
        if (voterId != Player.PlayerId || sourceVotedForId >= 253 || !Player.IsAlive()) return true;

        if (LastVoted == roleVoteFor)
        {
            MeetingHudPatch.TryAddAfterMeetingDeathPlayers(CustomDeathReason.Suicide, Player.PlayerId);
            Utils.GetPlayerById(sourceVotedForId).SetRealKiller(Player);
            MeetingVoteManager.Instance.ClearAndExile(Player.PlayerId, sourceVotedForId);
            return false;
        }
        else
        {
            Utils.SendMessage(Translator.GetString("DictatorOnVote"), Player.PlayerId);
            LastVoted = roleVoteFor;
            clearVote = true;
            return true;
        }
    }
}