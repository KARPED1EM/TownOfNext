using AmongUs.GameOptions;

using TOHE.Roles.Core;
using TOHE.Roles.Core.Interfaces;
using static TOHE.GuesserHelper;

namespace TOHE.Roles.Crewmate;
public sealed class NiceGuesser : RoleBase, IMeetingButton
{
    public static readonly SimpleRoleInfo RoleInfo =
        new(
            typeof(NiceGuesser),
            player => new NiceGuesser(player),
            CustomRoles.NiceGuesser,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            20000,
            SetupOptionItem,
            "ng",
            "#eede26"
        );
    public NiceGuesser(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    public static OptionItem OptionGuessNums;
    public static OptionItem OptionCanGuessCrew;
    public static OptionItem OptionCanGuessAddons;
    public static OptionItem OptionCanGuessVanilla;
    public static OptionItem OptionHideMsg;
    enum OptionName
    {
        GuesserCanGuessTimes,
        GGCanGuessCrew,
        GGCanGuessAdt,
        GGCanGuessVanilla,
        GGGuesserTryHideMsg,
    }

    public int GuessLimit;
    private static void SetupOptionItem()
    {
        OptionGuessNums = IntegerOptionItem.Create(RoleInfo, 10, OptionName.GuesserCanGuessTimes, new(1, 15, 1), 15, false)
            .SetValueFormat(OptionFormat.Times);
        OptionCanGuessCrew = BooleanOptionItem.Create(RoleInfo, 11, OptionName.GGCanGuessCrew, true, false);
        OptionCanGuessAddons = BooleanOptionItem.Create(RoleInfo, 12, OptionName.GGCanGuessAdt, false, false);
        OptionCanGuessVanilla = BooleanOptionItem.Create(RoleInfo, 13, OptionName.GGCanGuessVanilla, true, false);
        OptionHideMsg = BooleanOptionItem.Create(RoleInfo, 15, OptionName.GGGuesserTryHideMsg, true, false);
    }
    public override void Add()
    {
        GuessLimit = OptionGuessNums.GetInt();
    }
    public override void OverrideNameAsSeer(PlayerControl seen, ref string nameText, bool isForMeeting = false)
    {
        if (Player.IsAlive() && seen.IsAlive() && isForMeeting)
        {
            nameText = Utils.ColorString(RoleInfo.RoleColor, seen.PlayerId.ToString()) + " " + nameText;
        }
    }
    public string ButtonName { get; private set; } = "Target";
    public bool ShouldShowButton() => Player.IsAlive();
    public bool ShouldShowButtonFor(PlayerControl target) => target.IsAlive();
    public override bool OnSendMessage(string msg) => GuesserMsg(Player, msg);
    public bool OnClickButtonLocal(PlayerControl target)
    {
        ShowGuessPanel(target.PlayerId, MeetingHud.Instance);
        return false;
    }
}