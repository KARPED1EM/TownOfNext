using AmongUs.GameOptions;
using System.Collections.Generic;
using System.Linq;
using TOHE.Roles.Core;

namespace TOHE.Roles.Crewmate;
public sealed class CyberStar : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        new(
            typeof(CyberStar),
            player => new CyberStar(player),
            CustomRoles.CyberStar,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            8020176,
            SetupOptionItem,
            "se",
            "#ee4a55"
        );
    public CyberStar(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        MsgToSend = new();
    }

    public static OptionItem OptionImpKnow;
    public static OptionItem OptionNeutralKillerKnow;
    public static OptionItem OptionNeutralNonKillerKnow;
    enum OptionName
    {
        ImpKnowCyberStarDead,
        NeutralKillerKnowCyberStarDead,
        NeutralNonKillerKnowCyberStarDead,
    }

    private List<(string, byte, string)> MsgToSend;
    private static void SetupOptionItem()
    {
        OptionImpKnow = BooleanOptionItem.Create(RoleInfo, 10, OptionName.ImpKnowCyberStarDead, false, false);
        OptionNeutralKillerKnow = BooleanOptionItem.Create(RoleInfo, 10, OptionName.NeutralKillerKnowCyberStarDead, false, false);
        OptionNeutralNonKillerKnow = BooleanOptionItem.Create(RoleInfo, 10, OptionName.NeutralNonKillerKnowCyberStarDead, false, false);
    }
    public static bool CanSeeKillFlash(PlayerControl player)
    {
        return !player.IsAlive()
            || player.IsImp() && OptionImpKnow.GetBool()
            || player.IsNeutralKiller() && OptionNeutralKillerKnow.GetBool()
            || player.IsNeutralNonKiller() && OptionNeutralNonKillerKnow.GetBool();
    }
    public override void OnPlayerDeath(PlayerControl player, CustomDeathReason deathReason, bool isOnMeeting = false)
    {
        if (!Is(player)) return;

        foreach (var pc in Main.AllPlayerControls.Where(x => CanSeeKillFlash(x)))
        {
            if (isOnMeeting)
            {
                Utils.SendMessage(string.Format(Translator.GetString("CyberStarDead"), pc.GetRealName()), pc.PlayerId, Utils.ColorString(RoleInfo.RoleColor, Translator.GetString("CyberStarNewsTitle")));;
            }
            else
            {
                MsgToSend.Add((string.Format(Translator.GetString("CyberStarDead"), pc.GetRealName()), pc.PlayerId, Utils.ColorString(RoleInfo.RoleColor, Translator.GetString("CyberStarNewsTitle"))));
                pc.Notify(Utils.ColorString(RoleInfo.RoleColor, Translator.GetString("OnCyberStarDead")));
            }
        }
    }
    public override void NotifyOnMeetingStart(ref List<(string, byte, string)> msgToSend)
    {
        if (MsgToSend != null)
            foreach (var msg in MsgToSend) msgToSend.Add(msg);
        MsgToSend = new();
    }
}