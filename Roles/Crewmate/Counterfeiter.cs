using AmongUs.GameOptions;
using System.Collections.Generic;
using System.Linq;
using Hazel;
using HarmonyLib;
using UnityEngine;

using TOHE.Modules;
using TOHE.Roles.Core;
using TOHE.Roles.Core.Interfaces;

namespace TOHE.Roles.Crewmate;
public sealed class Counterfeiter : RoleBase, IKiller
{
    public static readonly SimpleRoleInfo RoleInfo =
        new(
            typeof(Counterfeiter),
            player => new Counterfeiter(player),
            CustomRoles.Counterfeiter,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Crewmate,
            21700,
            SetupOptionItem,
            "de",
            "#e0e0e0",
            true
        );
    public Counterfeiter(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False
    )
    {
        Customers = new();

        CustomRoleManager.OnCheckMurderPlayerOthers_Before.Add(OnCheckMurderPlayerOthers_Before);
    }

    static OptionItem OptionSellCooldown;
    static OptionItem OptionSellNums;
    enum OptionName
    {
        CounterfeiterSkillCooldown,
        CounterfeiterSkillLimitTimes,
    }

    private int SellLimit;
    private Dictionary<byte, bool> Customers;
    public bool IsKiller { get; private set; } = false;
    private static void SetupOptionItem()
    {
        OptionSellCooldown = FloatOptionItem.Create(RoleInfo, 10, OptionName.CounterfeiterSkillCooldown, new(2.5f, 180f, 2.5f), 20f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionSellNums = IntegerOptionItem.Create(RoleInfo, 11, OptionName.CounterfeiterSkillLimitTimes, new(1, 15, 1), 2, false)
            .SetValueFormat(OptionFormat.Times);
    }
    public override void Add()
    {
        var playerId = Player.PlayerId;
        SellLimit = OptionSellNums.GetInt();

        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    private void SendRPC()
    {
        using var sender = CreateSender(CustomRPC.SetCounterfeiterSellLimit);
        sender.Writer.Write(SellLimit);
    }
    public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        if (rpcType != CustomRPC.SetCounterfeiterSellLimit) return;
        SellLimit = reader.ReadInt32();
    }
    public float CalculateKillCooldown() => CanUseKillButton() ? OptionSellCooldown.GetFloat() : 255f;
    public bool CanUseKillButton() => Player.IsAlive() && SellLimit >= 1;
    public override bool CanSabotage(SystemTypes systemType) => false;
    public override void ApplyGameOptions(IGameOptions opt) => opt.SetVision(false);
    public bool OverrideKillButtonText(out string text)
    {
        text = Translator.GetString("CounterfeiterButtonText");
        return true;
    }
    public bool OnCheckMurderAsKiller(MurderInfo info)
    {
        if (SellLimit < 1) return false;
        var (killer, target) = info.AttemptTuple;
        
        if (Customers.ContainsKey(target.PlayerId))
        {
            killer.Notify(Translator.GetString("CounterfeiterRepeatSell"));
            return false;
        }

        SellLimit--;
        SendRPC();

        killer.ResetKillCooldown();
        killer.SetKillCooldownV2();
        killer.RPCPlayCustomSound("Bet");

        Customers.Add(target.PlayerId, false);

        Logger.Info($"{killer.GetNameWithRole()}：将赝品售卖给 => {target.GetNameWithRole()}", "Counterfeiter.OnCheckMurderAsKille");
        Logger.Info($"{killer.GetNameWithRole()}：剩余{SellLimit}个赝品", "Counterfeiter.OnCheckMurderAsKille");
        return false;
    }
    private static bool OnCheckMurderPlayerOthers_Before(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;

        foreach(var deceiver in Main.AllPlayerControls.Where(x => x.Is(CustomRoles.Counterfeiter)))
        {
            if (deceiver.GetRoleClass() is not Counterfeiter roleClass) continue;
            if (roleClass.Customers.TryGetValue(killer.PlayerId, out var x) && x)
            {
                killer.SetRealKiller(deceiver);
                killer.SetDeathReason(CustomDeathReason.Misfire);
                killer.RpcMurderPlayerV2(killer);
                Logger.Info($"{deceiver.GetNameWithRole()} 的客户：{killer.GetNameWithRole()} 因使用赝品走火自杀", "Counterfeiter.OnCheckMurderPlayerOthers_Before");
                return false;
            }
        }
        return true;
    }
    public override void OnStartMeeting()
    {
        var keys = Customers.Keys;
        keys.Do(x => Customers[x] = true);
       foreach (var pcId in Customers.Keys)
        {
            var target = Utils.GetPlayerById(pcId);
            if (target == null || !target.IsAlive()) continue;
            if (target.GetRoleClass() is IKiller x && x.IsKiller && x.CanKill) continue;
            CheckForEndVotingPatch.TryAddAfterMeetingDeathPlayers(CustomDeathReason.Misfire, target.PlayerId);
            target.SetRealKiller(Player);
            Logger.Info($"赝品商 {Player.GetRealName()} 的客户 {target.GetRealName()} 因不带刀将在会议结束后自杀", "Counterfeiter.OnStartMeeting");
        }
    }
    public override string GetProgressText(bool comms = false) => Utils.ColorString(CanUseKillButton() ? RoleInfo.RoleColor : Color.gray, $"({SellLimit})");
}