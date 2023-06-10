using AmongUs.GameOptions;
using System.Collections.Generic;
using System.Linq;

using TOHE.Roles.Core;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;
public sealed class Mediumshiper : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        new(
            typeof(Mediumshiper),
            player => new Mediumshiper(player),
            CustomRoles.Mediumshiper,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            22500,
            SetupOptionItem,
            "me",
            "#a200ff"
        );
    public Mediumshiper(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        CustomRoleManager.ReceiveMessage.Add(OnReceiveMessage);
    }

    static OptionItem OptionContactNums;
    static OptionItem OptionOnlyReceiveMsgFromCrew;
    enum OptionName
    {
        MediumshiperContactLimit,
        MediumshiperOnlyReceiveMsgFromCrew,
    }

    private int ContactLimit;
    private byte ContactPlayer;
    private static void SetupOptionItem()
    {
        OptionContactNums = IntegerOptionItem.Create(RoleInfo, 10, OptionName.MediumshiperContactLimit, new(1, 15, 1), 15,false)
            .SetValueFormat(OptionFormat.Times);
        OptionOnlyReceiveMsgFromCrew = BooleanOptionItem.Create(RoleInfo, 11, OptionName.MediumshiperOnlyReceiveMsgFromCrew, true, false);
    }
    public override void Add()
    {
        ContactLimit = OptionContactNums.GetInt();
        ContactPlayer = byte.MaxValue;
    }
    public override void OnReportDeadBody(PlayerControl reporter, GameData.PlayerInfo target)
    {
        ContactPlayer = byte.MaxValue;
        if (target == null || Player.PlayerId == target.PlayerId || !Player.IsAlive() || ContactLimit < 1) return;

        ContactLimit--;
        ContactPlayer = target.PlayerId;
        
        Logger.Info($"通灵师建立联系：{Player.GetNameWithRole()} => {target.PlayerName}", "Mediumshiper.OnReportDeadBody");
    }
    public override void NotifyOnMeetingStart(ref List<(string, byte, string)> msgToSend)
    {
        var target = Utils.GetPlayerById(ContactPlayer);
        if (ContactPlayer == byte.MaxValue || target == null) return;
        msgToSend.Add((
            string.Format(GetString("MediumshipNotifySelf"), target.GetRealName(), ContactLimit),
            Player.PlayerId,
            Utils.ColorString(RoleInfo.RoleColor, GetString("MediumshipTitle"))
            ));

        if (OptionOnlyReceiveMsgFromCrew.GetBool() && !target.IsCrew()) return;
        msgToSend.Add((
            string.Format(GetString("MediumshipNotifyTarget"), Player.GetRealName()),
            target.PlayerId,
            Utils.ColorString(RoleInfo.RoleColor, GetString("MediumshipTitle"))
            ));
    }
    private static bool OnReceiveMessage(PlayerControl player, string msg)
    {
        if (player.IsAlive()) return true;
        foreach (var medium in Main.AllAlivePlayerControls.Where(x => x.Is(CustomRoles.Mediumshiper)))
        {
            var roleClass = medium.GetRoleClass() as Mediumshiper;
            if (roleClass.ContactPlayer != player.PlayerId) continue;
            if (OptionOnlyReceiveMsgFromCrew.GetBool() && !player.IsCrew()) continue;

            msg = msg.ToLower().Trim();
            if (!CheckCommond(ref msg, "通灵|ms|mediumship|medium", false)) return true;

            bool ans;
            if (msg.Contains('n') || msg.Contains(GetString("No")) || msg.Contains('错') || msg.Contains("不是")) ans = false;
            else if (msg.Contains('y') || msg.Contains(GetString("Yes")) || msg.Contains('对')) ans = true;
            else
            {
                Utils.SendMessage(GetString("MediumshipHelp"), player.PlayerId);
                return false;
            }

            Utils.SendMessage(GetString("Mediumship" + (ans ? "Yes" : "No")), medium.PlayerId, Utils.ColorString(RoleInfo.RoleColor, GetString("MediumshipTitle")));
            Utils.SendMessage(GetString("MediumshipDone"), player.PlayerId, Utils.ColorString(RoleInfo.RoleColor, GetString("MediumshipTitle")));

            roleClass.ContactPlayer = byte.MaxValue;
            return false;
        }
        return true;
    }
    public static bool CheckCommond(ref string msg, string command, bool exact = true)
    {
        var comList = command.Split('|');
        for (int i = 0; i < comList.Count(); i++)
        {
            if (exact)
            {
                if (msg == "/" + comList[i]) return true;
            }
            else
            {
                if (msg.StartsWith("/" + comList[i]))
                {
                    msg = msg.Replace("/" + comList[i], string.Empty);
                    return true;
                }
            }
        }
        return false;
    }
}