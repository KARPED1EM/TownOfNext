using AmongUs.GameOptions;
using Hazel;

using TOHE.Roles.Core;
using TOHE.Roles.Core.Interfaces;
using UnityEngine;

// 来源：https://github.com/Yumenopai/TownOfHost_Y
namespace TOHE.Roles.Impostor;
public sealed class CursedWolf : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(CursedWolf),
            player => new CursedWolf(player),
            CustomRoles.CursedWolf,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            3500,
            SetupOptionItem,
            "cw|咒狼"
        );
    public CursedWolf(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    static OptionItem OptionGuardSpellTimes;
    enum OptionName
    {
        GuardSpellTimes,
    }

    private int SpellLimit;
    private static void SetupOptionItem()
    {
        OptionGuardSpellTimes = IntegerOptionItem.Create(RoleInfo, 10, OptionName.GuardSpellTimes, new(1, 99, 1), 3, false)
            .SetValueFormat(OptionFormat.Times);
    }
    public override void Add()
    {
        SpellLimit = OptionGuardSpellTimes.GetInt();
    }
    private void SendRPC()
    {
        using var sender = CreateSender(CustomRPC.SetCursedWolfSpellCount);
        sender.Writer.Write(SpellLimit);
    }
    public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        if (rpcType != CustomRPC.SetCursedWolfSpellCount) return;
        SpellLimit = reader.ReadInt32();
    }
    public override bool OnCheckMurderAsTarget(MurderInfo info)
    {
        if (SpellLimit < 1) return true;
        var (killer, target) = info.AttemptTuple;

        SpellLimit--;
        SendRPC();

        killer.RpcGuardAndKill(target);
        target.RpcGuardAndKill(target);

        killer.SetDeathReason(CustomDeathReason.Spell);
        target.RpcMurderPlayerV2(killer);

        target.Notify(Translator.GetString("CursedWolfSkill"));

        Logger.Info($"{target.GetNameWithRole()} 呪狼反杀 => {killer.GetNameWithRole()}", "CursedWolf.OnCheckMurderAsTarget");
        Logger.Info($"{target.GetNameWithRole()} 呪狼反杀：剩余{SpellLimit}次", "CursedWolf.OnCheckMurderAsTarget");
        return false;
    }
    public override string GetProgressText(bool comms = false) => Utils.ColorString(SpellLimit >= 1 ? Color.red : Color.gray, $"({SpellLimit})");
}