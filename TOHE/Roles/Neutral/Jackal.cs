using AmongUs.GameOptions;
using System.Linq;

using TOHE.Roles.Core;
using TOHE.Roles.Core.Interfaces;

namespace TOHE.Roles.Neutral;
public sealed class Jackal : RoleBase, IKiller
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Jackal),
            player => new Jackal(player),
            CustomRoles.Jackal,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Neutral,
            50900,
            SetupOptionItem,
            "jac",
            "#00b4eb"
        );
    public Jackal(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False,
        CountTypes.Jackal
    )
    {
        KillCooldown = OptionKillCooldown.GetFloat();
        CanVent = OptionCanVent.GetBool();
        CanUseSabotage = OptionCanUseSabotage.GetBool();
        WinBySabotage = OptionCanWinBySabotageWhenNoImpAlive.GetBool();
        HasImpostorVision = OptionHasImpostorVision.GetBool();
        ResetKillCooldown = OptionResetKillCooldownWhenSbGetKilled.GetBool();

        CustomRoleManager.OnMurderPlayerOthers.Add(OnMurderPlayerOthers);
    }

    private static OptionItem OptionKillCooldown;
    public static OptionItem OptionCanVent;
    public static OptionItem OptionCanUseSabotage;
    public static OptionItem OptionCanWinBySabotageWhenNoImpAlive;
    private static OptionItem OptionHasImpostorVision;
    private static OptionItem OptionResetKillCooldownWhenSbGetKilled;
    enum OptionName
    {
        JackalCanWinBySabotageWhenNoImpAlive,
        ResetKillCooldownWhenPlayerGetKilled,
    }
    private static float KillCooldown;
    public static bool CanVent;
    public static bool CanUseSabotage;
    public static bool WinBySabotage;
    private static bool HasImpostorVision;
    private static bool ResetKillCooldown;
    private static void SetupOptionItem()
    {
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(2.5f, 180f, 2.5f), 20f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionCanVent = BooleanOptionItem.Create(RoleInfo, 11, GeneralOption.CanVent, true, false);
        OptionCanUseSabotage = BooleanOptionItem.Create(RoleInfo, 12, GeneralOption.CanUseSabotage, false, false);
        OptionCanWinBySabotageWhenNoImpAlive = BooleanOptionItem.Create(RoleInfo, 14, OptionName.JackalCanWinBySabotageWhenNoImpAlive, true, false, OptionCanUseSabotage);
        OptionHasImpostorVision = BooleanOptionItem.Create(RoleInfo, 13, GeneralOption.ImpostorVision, true, false);
        OptionResetKillCooldownWhenSbGetKilled = BooleanOptionItem.Create(RoleInfo, 15, OptionName.ResetKillCooldownWhenPlayerGetKilled, true, false);
    }
    public float CalculateKillCooldown() => KillCooldown;
    public override void ApplyGameOptions(IGameOptions opt) => opt.SetVision(HasImpostorVision);
    public static void SetHudActive(HudManager __instance, bool isActive)
    {
        __instance.SabotageButton.ToggleVisible(isActive && CanUseSabotage);
    }
    public override bool CanSabotage(SystemTypes systemType) => CanUseSabotage;
    public static void OnMurderPlayerOthers(MurderInfo info)
    {
        if (!ResetKillCooldown || info.IsSuicide || info.IsAccident) return;
        foreach (var pc in Main.AllAlivePlayerControls.Where(x => x.Is(CustomRoles.Jackal) && x.PlayerId != info.AttemptKiller.PlayerId))
        {
            pc.SetKillCooldownV2(0);
            RPC.PlaySoundRPC(pc.PlayerId, Sounds.ImpTransform);
            pc.Notify(Translator.GetString("JackalResetKillCooldown"));
        }
    }
}