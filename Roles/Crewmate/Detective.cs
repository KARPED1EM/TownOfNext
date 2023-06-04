using AmongUs.GameOptions;
using System.Collections.Generic;

using TOHE.Roles.Core;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;
public sealed class Detective : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        new(
            typeof(Detective),
            player => new Detective(player),
            CustomRoles.Detective,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            8021015,
            SetupOptionItem,
            "de",
            "#7160e8"
        );
    public Detective(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        MsgToSend = new();
    }

    static OptionItem OptionKnowKiller;
    enum OptionName
    {
        DetectiveCanknowKiller,
    }

    private (string, byte, string) MsgToSend;
    private static void SetupOptionItem()
    {
        OptionKnowKiller = BooleanOptionItem.Create(RoleInfo, 10, OptionName.DetectiveCanknowKiller, true, false);
    }
    public override void OnReportDeadBody(PlayerControl reporter, GameData.PlayerInfo target)
    {
        var tpc = target.Object;
        if (!Is(reporter) || target == null || tpc  == null  || reporter.PlayerId == target.PlayerId) return;
        {
            string msg;
            msg = string.Format(GetString("DetectiveNoticeVictim"), tpc.GetRealName(), tpc.GetTrueRoleName());
            if (OptionKnowKiller.GetBool())
            {
                var realKiller = tpc.GetRealKiller();
                if (realKiller == null) msg += "；" + GetString("DetectiveNoticeKillerNotFound");
                else msg += "；" + string.Format(GetString("DetectiveNoticeKiller"), realKiller.GetTrueRoleName());
            }
            MsgToSend = (msg, Player.PlayerId, Utils.ColorString(RoleInfo.RoleColor, GetString("DetectiveNoticeTitle")));
        }
    }
    public override void NotifyOnMeetingStart(ref List<(string, byte, string)> msgToSend)
    {
        if (MsgToSend != (null, null, null)) msgToSend.Add(MsgToSend);
        MsgToSend = new();
    }
}