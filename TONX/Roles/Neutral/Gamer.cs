using Hazel;
using TONX.Roles.Core.Interfaces;
using TONX.Roles.Core;
using UnityEngine;
using AmongUs.GameOptions;
using System.Collections.Generic;
using HarmonyLib;
using System;

namespace TONX.Roles.Neutral;
public sealed class Gamer : RoleBase, IKiller
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Gamer),
            player => new Gamer(player),
            CustomRoles.Gamer,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Neutral,
            51300,
            SetupOptionItem,
            "dm",
            "#68bc71",
            true
        );
    public Gamer(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False,
        CountTypes.Gamer
    )
    {
        CanVent = OptionCanVent.GetBool();
    }

    private static OptionItem OptionKillCooldown;
    public static OptionItem OptionCanVent;
    private static OptionItem OptionHasImpostorVision;
    private static OptionItem OptionHealthMax;
    private static OptionItem OptionDamage;
    private static OptionItem OptionSelfHealthMax;
    private static OptionItem OptionSelfDamage;
    enum OptionName
    {
        GamerKillCooldown,
        GamerHealthMax,
        GamerDamage,
        GamerSelfHealthMax,
        GamerSelfDamage,
    }

    public static bool CanVent;
    private static Dictionary<byte, int> PlayerHP;
    private int GamerHP;

    private static void SetupOptionItem()
    {
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, OptionName.GamerKillCooldown, new(1f, 180f, 1f), 2f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionCanVent = BooleanOptionItem.Create(RoleInfo, 11, GeneralOption.CanVent, true, false);
        OptionHasImpostorVision = BooleanOptionItem.Create(RoleInfo, 12, GeneralOption.ImpostorVision, false, false);
        OptionHealthMax = IntegerOptionItem.Create(RoleInfo, 13, OptionName.GamerHealthMax, new(5, 990, 5), 100, false)
            .SetValueFormat(OptionFormat.Health);
        OptionDamage = IntegerOptionItem.Create(RoleInfo, 14, OptionName.GamerDamage, new(1, 100, 1), 15, false)
            .SetValueFormat(OptionFormat.Health);
        OptionSelfHealthMax = IntegerOptionItem.Create(RoleInfo, 15, OptionName.GamerSelfHealthMax, new(100, 100, 5), 100, false)
            .SetValueFormat(OptionFormat.Health);
        OptionSelfDamage = IntegerOptionItem.Create(RoleInfo, 16, OptionName.GamerSelfDamage, new(1, 100, 1), 35, false)
            .SetValueFormat(OptionFormat.Health);
    }
    public override void Add()
    {
        GamerHP = OptionSelfHealthMax.GetInt();
        PlayerHP = new();
        Main.AllPlayerControls.Do(p => PlayerHP.Add(p.PlayerId, OptionHealthMax.GetInt()));
    }
    public float CalculateKillCooldown() => OptionKillCooldown.GetFloat();
    public override void ApplyGameOptions(IGameOptions opt) => opt.SetVision(OptionHasImpostorVision.GetBool());
    public static void SetHudActive(HudManager __instance, bool isActive) => __instance.SabotageButton.ToggleVisible(false);
    public bool CanUseKillButton() => Player.IsAlive();
    private void SendRPC(byte id)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetGamerHealth, SendOption.Reliable, -1);
        writer.Write(Player.PlayerId);
        writer.Write(id);
        writer.Write(Player.PlayerId == id ? GamerHP : PlayerHP[id]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        if (rpcType != CustomRPC.SetGamerHealth) return;
        byte id = reader.ReadByte();
        int hp = reader.ReadInt32();
        if (Player.PlayerId == id)
            GamerHP = hp;
        else
            PlayerHP[id] = hp;
    }
    public bool OnCheckMurderAsKiller(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        if (info.IsSuicide) return true;
        if (!PlayerHP.ContainsKey(target.PlayerId)) return false;

        killer.SetKillCooldownV2();

        if (PlayerHP[target.PlayerId] - OptionDamage.GetInt() < 1)
        {
            PlayerHP.Remove(target.PlayerId);
            killer.RpcMurderPlayerV2(target);
            return false;
        }

        PlayerHP[target.PlayerId] -= OptionDamage.GetInt();
        SendRPC(target.PlayerId);

        RPC.PlaySoundRPC(killer.PlayerId, Sounds.KillSound);
        Utils.NotifyRoles(killer);

        Logger.Info($"{killer.GetNameWithRole()} 对玩家 {target.GetNameWithRole()} 造成了 {OptionDamage.GetInt()} 点伤害", "Gamer");
        return false;
    }
    public override bool OnCheckMurderAsTarget(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        if (info.IsSuicide) return true;

        if (GamerHP - OptionDamage.GetInt() < 1) return true;

        GamerHP -= OptionSelfDamage.GetInt();
        SendRPC(Player.PlayerId);

        RPC.PlaySoundRPC(target.PlayerId, Sounds.KillSound);
        killer.SetKillCooldownV2(target: target, forceAnime: true);
        Utils.NotifyRoles(target);

        Logger.Info($"{killer.GetNameWithRole()} 对玩家 {target.GetNameWithRole()} 造成了 {OptionSelfDamage.GetInt()} 点伤害", "Gamer");
        return false;
    }
    public override string GetSuffix(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        if (!Player.IsAlive()) return "";
        int max = OptionSelfHealthMax.GetInt();
        int now = GamerHP;
        if (seen != null)
        {
            max = OptionHealthMax.GetInt();
            now = Math.Max(1, PlayerHP.TryGetValue(seen.PlayerId, out var hp) ? hp : 1);
        }
        return Utils.ColorString(GetColor(now, seen == null), $"【{now}/{max}】");
    }
    private static Color32 GetColor(float Health, bool self = false)
    {
        var hpGradient = new NameTagManager.ColorGradient(new Color32(255, 0, 0, 255), new Color32(0, 255, 0, 255));
        return hpGradient.Evaluate(Health / (self ? OptionSelfHealthMax.GetInt() : OptionHealthMax.GetInt()));
    }
    public bool OverrideKillButtonText(out string text)
    {
        text = Translator.GetString("GamerButtonText");
        return true;
    }
}