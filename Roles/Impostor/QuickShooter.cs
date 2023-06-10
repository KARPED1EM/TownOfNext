using System.Collections.Generic;
using System.Linq;
using Hazel;
using UnityEngine;
using AmongUs.GameOptions;

using TOHE.Roles.Core;
using TOHE.Roles.Core.Interfaces;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;
public sealed class QuickShooter : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        new(
            typeof(QuickShooter),
            player => new QuickShooter(player),
            CustomRoles.QuickShooter,
            () => RoleTypes.Shapeshifter,
            CustomRoleTypes.Impostor,
            3700,
            SetupOptionItem,
            "qs"
        );
    public QuickShooter(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    static OptionItem OptionKillCooldown;
    static OptionItem OptionMeetingReserved;
    static OptionItem OptionShapeshiftCooldown;
    enum OptionName
    {
        KillCooldown,
        QuickShooterShapeshiftCooldown,
        MeetingReserved,
    }

    private int ShotLimit;
    private bool Storaging;
    private static void SetupOptionItem()
    {
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, OptionName.KillCooldown, new(2.5f, 180f, 2.5f), 35f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionShapeshiftCooldown = FloatOptionItem.Create(RoleInfo,12, OptionName.QuickShooterShapeshiftCooldown, new(2.5f, 180f, 2.5f), 15f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionMeetingReserved = IntegerOptionItem.Create(RoleInfo, 14, OptionName.MeetingReserved, new(0, 15, 1), 2, false)
            .SetValueFormat(OptionFormat.Pieces);
    }
    public override void Add()
    {
        ShotLimit = 0;
    }
    private void SendRPC()
    {
        using var sender = CreateSender(CustomRPC.SetQuickShooterShotLimit);
        sender.Writer.Write(ShotLimit);
    }
    public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        if (rpcType != CustomRPC.SetQuickShooterShotLimit) return;
        ShotLimit = reader.ReadInt32();
    }
    public override string GetProgressText(bool comms = false) => Utils.ColorString(ShotLimit >= 1 ? Color.red : Color.gray, $"({ShotLimit})");
    public override bool OverrideAbilityButtonText(out string text)
    {
        text = GetString("QuickShooterShapeshiftText");
        return true;
    }
    public override void ChangeHudManager(HudManager __instance) => __instance.AbilityButton.SetUsesRemaining(ShotLimit);
    public override void OnShapeshift(PlayerControl target)
    {
        var shapeshifting = !Is(target);

        if (!AmongUsClient.Instance.AmHost) return;

        if (Player.killTimer < 1 && shapeshifting)
        {
            ShotLimit++;
            SendRPC();
            Storaging = true;
            Player.ResetKillCooldown();
            Player.SetKillCooldownV2();
            Player.Notify(GetString("QuickShooterStoraging"));
            Logger.Info($"{Utils.GetPlayerById(Player.PlayerId)?.GetNameWithRole()} : 剩余子弹{ShotLimit}发", "QuickShooter.OnShapeshift");
        }
    }
    public float CalculateKillCooldown()
    {
        float cooldown = (Storaging || ShotLimit < 1) ? OptionKillCooldown.GetFloat() : 0.01f;
        Storaging = false;
        return cooldown;
    }
    public override void OnStartMeeting()
    {
        int before = ShotLimit;
        ShotLimit = Mathf.Clamp(ShotLimit, 0, OptionMeetingReserved.GetInt());
        if(ShotLimit != before) SendRPC();
    }
    public void BeforeMurderPlayerAsKiller(MurderInfo info)
    {
        int before = ShotLimit;
        ShotLimit--;
        ShotLimit = Mathf.Max(ShotLimit, 0);
        if (ShotLimit != before) SendRPC();
    }
}
