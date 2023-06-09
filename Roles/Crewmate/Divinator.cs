using AmongUs.GameOptions;
using System.Collections.Generic;

using TOHE.Roles.Core;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;
public sealed class Divinator : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        new(
            typeof(Divinator),
            player => new Divinator(player),
            CustomRoles.Divinator,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            8022560,
            SetupOptionItem,
            "ft",
            "#882c83"
        );
    public Divinator(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        DidVote = false;
    }

    static OptionItem OptionCheckNums;
    static OptionItem OptionAccurateCheck;
    static OptionItem OptionHideVote;
    enum OptionName
    {
        DivinatorSkillLimit,
        AccurateCheckMode,
        DivinatorHideVote,
    }

    private int CheckLimit;
    private bool DidVote;
    private static void SetupOptionItem()
    {
        OptionCheckNums = IntegerOptionItem.Create(RoleInfo, 10, OptionName.DivinatorSkillLimit, new(1, 15, 1), 5, false)
            .SetValueFormat(OptionFormat.Times);
        OptionAccurateCheck = BooleanOptionItem.Create(RoleInfo, 11, OptionName.AccurateCheckMode, false, false);
        OptionHideVote = BooleanOptionItem.Create(RoleInfo, 12, OptionName.DivinatorHideVote, false, false);
        Options.OverrideTasksData.Create(RoleInfo, 20);
    }
    public override void Add() => CheckLimit = OptionCheckNums.GetInt();
    public override bool OnVotingEnd(ref List<MeetingHud.VoterState> statesList, ref PlayerVoteArea pva) => !OptionHideVote.GetBool();
    public override void OnStartMeeting() => DidVote = false;
    public override bool OnCheckForEndVoting(ref List<MeetingHud.VoterState> statesList, PlayerVoteArea pva)
    {
        if (pva.DidVote && pva.VotedFor < 253 && Player.IsAlive() && !DidVote)
        {
            DidVote = true;
            var target = Utils.GetPlayerById(pva.VotedFor);
            if (target == null) return true;

            if (CheckLimit < 1)
            {
                Utils.SendMessage(GetString("DivinatorCheckReachLimit"), Player.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Divinator), GetString("DivinatorCheckMsgTitle")));
                return true;
            }

            CheckLimit--;

            if (Is(target))
            {
                Utils.SendMessage(GetString("DivinatorCheckSelfMsg") + "\n\n" + string.Format(GetString("DivinatorCheckLimit"), CheckLimit), Player.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Divinator), GetString("DivinatorCheckMsgTitle")));
                return true;
            }

            string msg;

            if (Player.AllTasksCompleted() || OptionAccurateCheck.GetBool())
            {
                msg = string.Format(GetString("DivinatorCheck.TaskDone"), target.GetRealName(), GetString(target.GetCustomRole().ToString()));
            }
            else
            {
                string text = target.GetCustomRole() switch
                {
                    CustomRoles.TimeThief or
                    CustomRoles.AntiAdminer or
                    CustomRoles.SuperStar or
                    CustomRoles.Mayor or
                    CustomRoles.Snitch or
                    CustomRoles.Counterfeiter or
                    CustomRoles.God or
                    CustomRoles.Judge or
                    CustomRoles.Observer or
                    CustomRoles.DoveOfPeace
                    => "HideMsg",

                    CustomRoles.Miner or
                    CustomRoles.Scavenger or
                    CustomRoles.Luckey or
                    CustomRoles.Needy or
                    CustomRoles.SabotageMaster or
                    CustomRoles.Jackal or
                    CustomRoles.Mario or
                    CustomRoles.Cleaner or
                    CustomRoles.Crewpostor
                    => "Honest",

                    CustomRoles.SerialKiller or
                    CustomRoles.BountyHunter or
                    CustomRoles.Minimalism or
                    CustomRoles.Sans or
                    CustomRoles.SpeedBooster or
                    CustomRoles.Sheriff or
                    CustomRoles.Arsonist or
                    CustomRoles.Innocent or
                    CustomRoles.FFF or
                    CustomRoles.Greedier
                    => "Impulse",

                    CustomRoles.Vampire or
                    CustomRoles.Assassin or
                    CustomRoles.Escapee or
                    CustomRoles.Sniper or
                    CustomRoles.SwordsMan or
                    CustomRoles.Bodyguard or
                    CustomRoles.Opportunist or
                    CustomRoles.Pelican or
                    CustomRoles.ImperiusCurse
                    => "Weirdo",

                    CustomRoles.EvilGuesser or
                    CustomRoles.Bomber or
                    CustomRoles.Capitalism or
                    CustomRoles.NiceGuesser or
                    CustomRoles.Grenadier or
                    CustomRoles.Terrorist or
                    CustomRoles.Revolutionist or
                    CustomRoles.Gamer or
                    CustomRoles.Eraser
                    => "Blockbuster",

                    CustomRoles.Warlock or
                    CustomRoles.Hacker or
                    CustomRoles.Mafia or
                    CustomRoles.Doctor or
                    CustomRoles.Transporter or
                    CustomRoles.Veteran or
                    CustomRoles.Divinator or
                    CustomRoles.QuickShooter or
                    CustomRoles.Mediumshiper or
                    CustomRoles.Judge or
                    CustomRoles.BloodKnight
                    => "Strong",

                    CustomRoles.Witch or
                    CustomRoles.Puppeteer or
                    CustomRoles.ShapeMaster or
                    CustomRoles.Paranoia or
                    CustomRoles.Psychic or
                    CustomRoles.Executioner or
                    CustomRoles.BallLightning or
                    CustomRoles.Workaholic or
                    CustomRoles.Provocateur
                    => "Incomprehensible",

                    CustomRoles.FireWorks or
                    CustomRoles.EvilTracker or
                    CustomRoles.Gangster or
                    CustomRoles.Dictator or
                    CustomRoles.CyberStar or
                    CustomRoles.Collector or
                    CustomRoles.Sunnyboy or
                    CustomRoles.Bard or
                    CustomRoles.Totocalcio
                    => "Enthusiasm",

                    CustomRoles.BoobyTrap or
                    CustomRoles.Zombie or
                    CustomRoles.Mare or
                    CustomRoles.Detective or
                    CustomRoles.TimeManager or
                    CustomRoles.Jester or
                    CustomRoles.Medicaler or
                    CustomRoles.DarkHide or
                    CustomRoles.CursedWolf or
                    CustomRoles.OverKiller or
                    CustomRoles.Hangman or
                    CustomRoles.Mortician
                    => "Disturbed",

                    CustomRoles.Glitch or
                    CustomRoles.Concealer or
                    CustomRoles.Swooper
                    => "Glitch",

                    CustomRoles.Succubus
                    => "Love",

                    _ => "None",
                };
                msg = string.Format(GetString("DivinatorCheck." + text), target.GetRealName());
            }

            Utils.SendMessage(GetString("DivinatorCheck") + "\n" + msg + "\n\n" + string.Format(GetString("DivinatorCheckLimit"), CheckLimit), Player.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Divinator), GetString("DivinatorCheckMsgTitle")));
        }
        return true;
    }
}