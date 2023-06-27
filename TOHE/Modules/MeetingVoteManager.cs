using System;
using System.Collections.Generic;
using System.Linq;
using TOHE.Roles.AddOns.Common;
using TOHE.Roles.Core;

namespace TOHE.Modules;

public class MeetingVoteManager
{
    public IReadOnlyDictionary<byte, VoteData> AllVotes => allVotes;
    private Dictionary<byte, VoteData> allVotes = new(15);
    private readonly MeetingHud meetingHud;

    public static MeetingVoteManager Instance => _instance;
    private static MeetingVoteManager _instance;
    private static LogHandler logger = Logger.Handler(nameof(MeetingVoteManager));

    private MeetingVoteManager()
    {
        meetingHud = MeetingHud.Instance;
        ClearVotes();
    }

    public static void Start()
    {
        _instance = new();
    }

    /// <summary>
    /// 初始化投票
    /// </summary>
    public void ClearVotes()
    {
        foreach (var voteArea in meetingHud.playerStates)
        {
            allVotes[voteArea.TargetPlayerId] = new(voteArea.TargetPlayerId);
        }
    }
    /// <summary>
    /// 删除到目前为止的所有票，并以对特定投票者的一票结束会议
    /// </summary>
    /// <param name="voter">投票者</param>
    /// <param name="exiled">驱逐者</param>
    public void ClearAndExile(byte voter, byte exiled)
    {
        logger.Info($"{Utils.GetPlayerById(voter).GetNameWithRole()} によって {GetVoteName(exiled)} が追放されます");
        ClearVotes();
        var vote = new VoteData(voter);
        vote.DoVote(exiled, 1);
        allVotes[voter] = vote;
        EndMeeting(false);
    }
    /// <summary>
    /// 新增投票数据
    /// </summary>
    /// <param name="voter">投票者</param>
    /// <param name="voteFor">投票目标</param>
    /// <param name="numVotes">票数</param>
    /// <returns>false: 请撤销本次投票以允许投票者重新投票</returns>
    public bool AddVote(byte voter, byte voteFor, int numVotes = 1)
    {
        if (!allVotes.TryGetValue(voter, out var vote))
        {
            logger.Warn($"ID: {voter} 没有存在的投票数据，创建新的投票数据");
            vote = new(voter);
        }
        if (vote.HasVoted)
        {
            logger.Info($"ID: {voter} 已经存在投票数据，覆盖原先的投票数据");
        }

        bool doVote = true;
        bool clearVote = false;

        foreach (var role in CustomRoleManager.AllActiveRoles.Values)
        {
            var (roleVoteFor, roleNumVotes) = (voteFor, numVotes);
            doVote = role.OnVote(voter, voteFor, ref roleVoteFor, ref roleNumVotes, ref clearVote);
            if (roleVoteFor != voteFor)
            {
                logger.Info($"{role.Player.GetNameWithRole()} 将 {Utils.GetPlayerById(voter).GetNameWithRole()} 的投票目标修改为 {GetVoteName(roleVoteFor)}");
                voteFor = roleVoteFor;
            }
            if (roleNumVotes != numVotes)
            {
                logger.Info($"{role.Player.GetNameWithRole()} 将 {Utils.GetPlayerById(voter).GetNameWithRole()} 的票数修改为 {roleNumVotes}");
                numVotes = roleNumVotes;
            }
            if (!doVote)
            {
                logger.Info($"{role.Player.GetNameWithRole()} 阻塞了投票，本次投票数据不计入");
            }
            if (clearVote)
            {
                logger.Info($"{role.Player.GetNameWithRole()} 撤销了投票，允许重新投票");
            }
        }

        if (doVote) vote.DoVote(voteFor, numVotes);
        return !clearVote;
    }
    /// <summary>
    /// 如果会议时间耗尽或每个人都已投票，则结束会议
    /// </summary>
    public void CheckAndEndMeeting()
    {
        if (meetingHud.discussionTimer - Main.NormalOptions.DiscussionTime >= Main.NormalOptions.VotingTime || AllVotes.Values.All(vote => vote.HasVoted))
        {
            EndMeeting();
        }
    }
    /// <summary>
    /// 无条件终止会议
    /// </summary>
    /// <param name="applyVoteMode">是否应用投票的设置</param>
    public void EndMeeting(bool applyVoteMode = true)
    {
        var result = CountVotes(applyVoteMode);
        var logName = result.Exiled == null ? (result.IsTie ? "平票" : "跳过") : result.Exiled.Object.GetNameWithRole();
        logger.Info($"会议结束，结果：{logName}");

        var states = new List<MeetingHud.VoterState>();
        foreach (var voteArea in meetingHud.playerStates)
        {
            var voteData = AllVotes.TryGetValue(voteArea.TargetPlayerId, out var value) ? value : null;
            if (voteData == null)
            {
                logger.Warn($"{Utils.GetPlayerById(voteArea.TargetPlayerId).GetNameWithRole()} 没有投票数据");
                continue;
            }
            for (var i = 0; i < voteData.NumVotes; i++)
            {
                states.Add(new()
                {
                    VoterId = voteArea.TargetPlayerId,
                    VotedForId = voteData.VotedFor,
                });
            }
        }

        if (AntiBlackout.OverrideExiledPlayer)
        {
            meetingHud.RpcVotingComplete(states.ToArray(), null, true);
            ExileControllerWrapUpPatch.AntiBlackout_LastExiled = result.Exiled;
        }
        else
        {
            meetingHud.RpcVotingComplete(states.ToArray(), result.Exiled, result.IsTie);
        }
        if (result.Exiled != null)
        {
            MeetingHudPatch.CheckForDeathOnExile(CustomDeathReason.Vote, result.Exiled.PlayerId);
            ConfirmEjections.Eject(result.Exiled);
        }
        Destroy();
    }
    /// <summary>
    /// <see cref="AllVotes"/>から投票をカウントします
    /// </summary>
    /// <param name="applyVoteMode">是否应用投票的设置</param>
    /// <returns>([Key: 被票者,Value: 票数]的字典，被驱逐者，是否平票)</returns>
    public VoteResult CountVotes(bool applyVoteMode)
    {
        // 根据投票设置修改结果
        if (applyVoteMode && Options.VoteMode.GetBool())
        {
            ApplySkipAndNoVoteMode();
        }

        // Key: 被投票的人
        // Value: 票数
        Dictionary<byte, int> votes = new();
        foreach (var voteArea in meetingHud.playerStates)
        {
            votes[voteArea.TargetPlayerId] = 0;
        }
        votes[Skip] = 0;
        foreach (var vote in AllVotes.Values)
        {
            if (vote.VotedFor == NoVote)
            {
                continue;
            }
            votes[vote.VotedFor] += vote.NumVotes;
        }

        return new VoteResult(votes);
    }
    /// <summary>
    /// 根据跳过模式和无投票模式更改投票或杀死玩家
    /// </summary>
    private void ApplySkipAndNoVoteMode()
    {
        var ignoreSkipModeDueToFirstMeeting = MeetingStates.FirstMeeting && Options.WhenSkipVoteIgnoreFirstMeeting.GetBool();
        var ignoreSkipModeDueToNoDeadBody = !MeetingStates.IsExistDeadBody && Options.WhenSkipVoteIgnoreNoDeadBody.GetBool();
        var ignoreSkipModeDueToEmergency = MeetingStates.IsEmergencyMeeting && Options.WhenSkipVoteIgnoreEmergency.GetBool();
        var ignoreSkipMode = ignoreSkipModeDueToFirstMeeting || ignoreSkipModeDueToNoDeadBody || ignoreSkipModeDueToEmergency;

        var skipMode = Options.GetWhenSkipVote();
        var noVoteMode = Options.GetWhenNonVote();
        foreach (var voteData in AllVotes)
        {
            var vote = voteData.Value;
            if (!vote.HasVoted)
            {
                var voterName = Utils.GetPlayerById(vote.Voter).GetNameWithRole();
                switch (noVoteMode)
                {
                    case VoteMode.Suicide:
                        MeetingHudPatch.TryAddAfterMeetingDeathPlayers(CustomDeathReason.Suicide, vote.Voter);
                        logger.Info($"根据房间设定，{voterName} 因未投票自杀");
                        break;
                    case VoteMode.SelfVote:
                        vote.ChangeVoteTarget(vote.Voter);
                        logger.Info($"根据房间设定，{voterName} 未投票算作自票");
                        break;
                    case VoteMode.Skip:
                        vote.ChangeVoteTarget(Skip);
                        logger.Info($"根据房间设定，{voterName} 未投票算作跳过");
                        break;
                }
            }
            else if (!ignoreSkipMode && vote.IsSkip)
            {
                var voterName = Utils.GetPlayerById(vote.Voter).GetNameWithRole();
                switch (skipMode)
                {
                    case VoteMode.Suicide:
                        MeetingHudPatch.TryAddAfterMeetingDeathPlayers(CustomDeathReason.Suicide, vote.Voter);
                        logger.Info($"根据房间设定，{voterName} 因跳过投票自杀");
                        break;
                    case VoteMode.SelfVote:
                        vote.ChangeVoteTarget(vote.Voter);
                        logger.Info($"根据房间设定，{voterName} 跳过投票算作自票");
                        break;
                }
            }
        }
    }
    public void Destroy()
    {
        _instance = null;
    }

    public static string GetVoteName(byte num)
    {
        string name = "invalid";
        var player = Utils.GetPlayerById(num);
        if (num < 15 && player != null) name = player?.GetNameWithRole();
        else if (num == Skip) name = "Skip";
        else if (num == NoVote) name = "None";
        else if (num == 255) name = "Dead";
        return name;
    }

    public class VoteData
    {
        public byte Voter { get; private set; } = byte.MaxValue;
        public byte VotedFor { get; private set; } = NoVote;
        public int NumVotes { get; private set; } = 1;
        public bool IsSkip => VotedFor == Skip && !PlayerState.GetByPlayerId(Voter).IsDead;
        public bool HasVoted => VotedFor != NoVote || PlayerState.GetByPlayerId(Voter).IsDead;

        public VoteData(byte voter) => Voter = voter;

        public void DoVote(byte voteTo, int numVotes)
        {
            logger.Info($"投票：{Utils.GetPlayerById(Voter).GetNameWithRole()} => {GetVoteName(voteTo)} x {numVotes}");
            VotedFor = voteTo;
            NumVotes = numVotes;
            Brakar.OnVote(Voter, voteTo);
        }
        public void ChangeVoteTarget(byte voteTarget)
        {
            logger.Info($"{Utils.GetPlayerById(Voter).GetNameWithRole()} 的投票目标由 {GetVoteName(VotedFor)} 变为 {GetVoteName(voteTarget)}");
            VotedFor = voteTarget;
            Brakar.OnVote(Voter, voteTarget);
        }
    }

    public readonly struct VoteResult
    {
        /// <summary>
        /// Key: 被票者<br/>
        /// Value: 得票数
        /// </summary>
        public IReadOnlyDictionary<byte, int> VotedCounts => votedCounts;
        private readonly Dictionary<byte, int> votedCounts;
        /// <summary>
        /// 驱逐玩家
        /// </summary>
        public readonly GameData.PlayerInfo Exiled;
        /// <summary>
        /// 是否平票
        /// </summary>
        public readonly bool IsTie;

        public VoteResult(Dictionary<byte, int> votedCounts)
        {
            this.votedCounts = votedCounts;

            // 按照票数排序
            var orderedVotes = votedCounts.OrderByDescending(vote => vote.Value);
            // 得票最多的人的票数
            var maxVoteNum = orderedVotes.FirstOrDefault().Value;
            // 所有得票最多的玩家
            var mostVotedPlayers = votedCounts.Where(vote => vote.Value == maxVoteNum).Select(vote => vote.Key).ToArray();

            // 存在相同最多票数的玩家
            if (mostVotedPlayers.Length > 1)
            {
                IsTie = true;
                Exiled = null;
                logger.Info($"{string.Join('，', mostVotedPlayers.Select(GetVoteName))} 平票");
            }
            else
            {
                IsTie = false;
                Exiled = GameData.Instance.GetPlayerById(mostVotedPlayers[0]);
                logger.Info($"得票最多者：{GetVoteName(mostVotedPlayers[0])}");
            }

            if (IsTie && Brakar.ChooseExileTarget(mostVotedPlayers, out var brakarTarget))
            {
                IsTie = false;
                Exiled = GameData.Instance.GetPlayerById(brakarTarget);
            }

            // 同数投票時の特殊モード
            if (IsTie && Options.VoteMode.GetBool())
            {
                var tieMode = (TieMode)Options.WhenTie.GetValue();
                switch (tieMode)
                {
                    case TieMode.All:
                        var toExile = mostVotedPlayers.Where(id => id != Skip).ToArray();
                        foreach (var playerId in toExile)
                        {
                            Utils.GetPlayerById(playerId)?.SetRealKiller(null);
                        }
                        MeetingHudPatch.TryAddAfterMeetingDeathPlayers(CustomDeathReason.Vote, toExile);
                        Exiled = null;
                        logger.Info("根据房间设定，平票玩家全部驱逐");
                        break;
                    case TieMode.Random:
                        var exileId = mostVotedPlayers.OrderBy(_ => Guid.NewGuid()).FirstOrDefault();
                        Exiled = GameData.Instance.GetPlayerById(exileId);
                        IsTie = false;
                        logger.Info($"根据房间设定，平票随机驱逐玩家：{GetVoteName(exileId)}");
                        break;
                }
            }
        }
    }

    public const byte Skip = 253;
    public const byte NoVote = 254;
}