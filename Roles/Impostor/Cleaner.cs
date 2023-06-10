using AmongUs.GameOptions;
using System.Collections.Generic;
using TOHE.Modules;
using TOHE.Roles.Core;
using TOHE.Roles.Core.Interfaces;

namespace TOHE.Roles.Impostor;
public sealed class Cleaner : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        new(
            typeof(Cleaner),
            player => new Cleaner(player),
            CustomRoles.Cleaner,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            3300,
            SetupOptionItem,
            "cl"
        );
    public Cleaner(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        BodiesCleanedUp = new();
    }

    static OptionItem OptionKillCooldown;
    static OptionItem OptionResetKillCooldownAfterClean;
    enum OptionName
    {
        CleanerResetKillCooldownAfterClean
    }

    private List<byte> BodiesCleanedUp;
    private static void SetupOptionItem()
    {
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(2.5f, 180f, 2.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionResetKillCooldownAfterClean = BooleanOptionItem.Create(RoleInfo, 11, OptionName.CleanerResetKillCooldownAfterClean, false, false);
    }
    public float CalculateKillCooldown() => OptionKillCooldown.GetFloat();
    public override bool OverrideAbilityButtonText(out string text)
    {
        text = Translator.GetString("MinerTeleButtonText");
        return true;
    }
    public override string GetReportButtonText() => Translator.GetString("CleanerReportButtonText");
    public override bool OnCheckReportDeadBody(PlayerControl reporter, GameData.PlayerInfo target)
    {
        if (BodiesCleanedUp.Contains(target.PlayerId))
        {
            reporter.Notify(Utils.ColorString(RoleInfo.RoleColor, Translator.GetString("ReportCleanedBodies")));
            Logger.Info($"{target.Object.GetNameWithRole()} 的尸体已被清理，无法被报告", "Cleaner.OnCheckReportDeadBody");
            return false;
        }
        if (!Is(reporter) || target == null) return true;
        ReportDeadBodyPatch.CanReport[target.PlayerId] = false;
        BodiesCleanedUp.Add(target.PlayerId);
        if (OptionResetKillCooldownAfterClean.GetBool()) Player.SetKillCooldownV2();
        Player.Notify(Translator.GetString("CleanerCleanBody"));
        Player.RPCPlayCustomSound("Clothe");
        return false;
    }
}