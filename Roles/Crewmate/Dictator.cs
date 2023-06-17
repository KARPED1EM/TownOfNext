using AmongUs.GameOptions;
using System.Collections.Generic;
using TOHE.Roles.Core;
using static TOHE.CheckForEndVotingPatch;

namespace TOHE.Roles.Crewmate;
public sealed class Dictator : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        new(
            typeof(Dictator),
            player => new Dictator(player),
            CustomRoles.Dictator,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            21200,
            null,
            "dic",
            "#df9b00"
        );
    public Dictator(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }
    public override bool OnCheckForEndVoting(ref List<MeetingHud.VoterState> statesList, PlayerVoteArea pva)
    {
        //死んでいないディクテーターが投票済み
        if (pva.DidVote &&
            pva.VotedFor != Player.PlayerId &&
            pva.VotedFor < 253 &&
            Player.IsAlive())
        {
            var voteTarget = Utils.GetPlayerById(pva.VotedFor);
            TryAddAfterMeetingDeathPlayers(CustomDeathReason.Suicide, Player.PlayerId);
            statesList.Add(new()
            {
                VoterId = pva.TargetPlayerId,
                VotedForId = pva.VotedFor
            });
            var states = statesList.ToArray();
            if (AntiBlackout.OverrideExiledPlayer)
            {
                MeetingHud.Instance.RpcVotingComplete(states, null, true);
                ExileControllerWrapUpPatch.AntiBlackout_LastExiled = voteTarget.Data;
            }
            else MeetingHud.Instance.RpcVotingComplete(states, voteTarget.Data, false); //通常処理

            CheckForDeathOnExile(CustomDeathReason.Vote, pva.VotedFor);
            Logger.Info($"独裁投票，会议强制结束 (驱逐：{voteTarget.GetNameWithRole()})", "Special Phase");
            voteTarget.SetRealKiller(Player);
            Main.LastVotedPlayerInfo = voteTarget.Data;
            if (Main.LastVotedPlayerInfo != null)
                ConfirmEjections(Main.LastVotedPlayerInfo);
        }
        return false;
    }
}