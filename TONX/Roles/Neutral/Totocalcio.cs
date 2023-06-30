using AmongUs.GameOptions;
using Hazel;
using System;
using System.Linq;
using TONX.Modules;
using TONX.Roles.Core;
using TONX.Roles.Core.Interfaces;
using UnityEngine;

namespace TONX.Roles.Neutral;
public sealed class Totocalcio : RoleBase, IKiller, IAdditionalWinner
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Totocalcio),
            player => new Totocalcio(player),
            CustomRoles.Totocalcio,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Neutral,
            51000,
            SetupOptionItem,
            "fo|賭徒",
            "#ff9409",
            true
        );
    public Totocalcio(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False
    )
    {
        CustomRoleManager.MarkOthers.Add(GetMarkOthers);
    }

    private static OptionItem OptionMaxBetTimes;
    public static OptionItem OptionBetCooldown;
    private static OptionItem OptionBetCooldownIncrese;
    private static OptionItem OptionMaxBetCooldown;
    private static OptionItem OptionKnowTargetRole;
    private static OptionItem OptionBetTargetKnowTotocalcio;
    enum OptionName
    {
        TotocalcioMaxBetTimes,
        TotocalcioBetCooldown,
        TotocalcioMaxBetCooldown,
        TotocalcioBetCooldownIncrese,
        TotocalcioKnowTargetRole,
        TotocalcioBetTargetKnowTotocalcio,
    }


    private int BetLimit;
    private byte BetTarget;

    private static void SetupOptionItem()
    {
        OptionMaxBetTimes = IntegerOptionItem.Create(RoleInfo, 10, OptionName.TotocalcioMaxBetTimes, new(1, 99, 1), 3, false)
            .SetValueFormat(OptionFormat.Times);
        OptionBetCooldown = FloatOptionItem.Create(RoleInfo, 11, OptionName.TotocalcioBetCooldown, new(2.5f, 180f, 2.5f), 10f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionMaxBetCooldown = FloatOptionItem.Create(RoleInfo, 12, OptionName.TotocalcioMaxBetCooldown, new(0f, 990f, 2.5f), 50f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionBetCooldownIncrese = FloatOptionItem.Create(RoleInfo, 13, OptionName.TotocalcioBetCooldownIncrese, new(0f, 60f, 1f), 4f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionKnowTargetRole = BooleanOptionItem.Create(RoleInfo, 14, OptionName.TotocalcioKnowTargetRole, false, false);
        OptionBetTargetKnowTotocalcio = BooleanOptionItem.Create(RoleInfo, 15, OptionName.TotocalcioBetTargetKnowTotocalcio, false, false);
    }
    public override void Add()
    {
        BetLimit = OptionMaxBetTimes.GetInt();
        BetTarget = byte.MaxValue;
    }
    public bool IsKiller => false;
    public float CalculateKillCooldown()
    {
        if (BetLimit < 1) return 255f;
        float cd = OptionBetCooldown.GetFloat();
        cd += Main.AllPlayerControls.Count(x => !x.IsAlive()) * OptionBetCooldownIncrese.GetFloat();
        cd = Math.Min(cd, OptionMaxBetCooldown.GetFloat());
        return cd;
    }
    public override void ApplyGameOptions(IGameOptions opt) => opt.SetVision(false);
    public static void SetHudActive(HudManager __instance, bool isActive) => __instance.SabotageButton.ToggleVisible(false);
    private void SendRPC()
    {
        var sender = CreateSender(CustomRPC.SyncTotocalcioTargetAndTimes);
        sender.Writer.Write(BetLimit);
        sender.Writer.Write(BetTarget);
    }
    public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        if (rpcType != CustomRPC.SyncTotocalcioTargetAndTimes) return;
        BetLimit = reader.ReadInt32();
        BetTarget = reader.ReadByte();
    }
    public bool CanUseKillButton() => Player.IsAlive() && BetLimit >= 1;
    public override void OverrideRoleNameAsSeer(PlayerControl seen, ref bool enabled, ref UnityEngine.Color roleColor, ref string roleText)
    {
        if (!OptionKnowTargetRole.GetBool()) return;
        if (seen.PlayerId == BetTarget) enabled = true;
    }
    public bool OnCheckMurderAsKiller(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        if (info.IsSuicide) return false;
        if (BetTarget == target.PlayerId) return false;
        if (BetLimit < 1) return false;

        BetLimit--;
        var beforeTarget = Utils.GetPlayerById(BetTarget);
        if (beforeTarget != null) Utils.NotifyRoles(beforeTarget);

        BetTarget = target.PlayerId;
        SendRPC();

        killer.ResetKillCooldown();
        killer.SetKillCooldownV2();
        killer.RPCPlayCustomSound("Bet");

        killer.Notify(Translator.GetString("TotocalcioBetPlayer"));
        if (OptionBetTargetKnowTotocalcio.GetBool())
            target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Totocalcio), Translator.GetString("TotocalcioBetOnYou")));

        Logger.Info($"赌徒下注：{killer.GetNameWithRole()} => {target.GetNameWithRole()}", "Totocalcio");

        return false;
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        return seen.PlayerId == BetTarget ? Utils.ColorString(Utils.GetRoleColor(CustomRoles.Totocalcio), "♦") : "";
    }
    private static string GetMarkOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        if (seen == null || !OptionBetTargetKnowTotocalcio.GetBool()) return "";
        return (seen.GetRoleClass() is Totocalcio roleClass && roleClass.BetTarget == seer.PlayerId)
            ? Utils.ColorString(Utils.GetRoleColor(CustomRoles.Totocalcio), "♦") : "";
    }
    public override string GetProgressText(bool comms = false) => Utils.ColorString(CanUseKillButton() ? Utils.ShadeColor(RoleInfo.RoleColor, 0.25f) : Color.gray, $"({BetLimit})");
    public bool CheckWin(out AdditionalWinners winnerType)
    {
        winnerType = AdditionalWinners.Totocalcio;
        if (BetTarget == byte.MaxValue) return false;
        var targetPs = PlayerState.GetByPlayerId(BetTarget);
        return (CustomWinnerHolder.WinnerIds?.Contains(BetTarget) ?? false)
            || (targetPs != null && (CustomWinnerHolder.WinnerRoles?.Contains(targetPs.MainRole) ?? false));
    }
    public bool OverrideKillButtonText(out string text)
    {
        text = Translator.GetString("TotocalcioKillButtonText");
        return true;
    }
}