using AmongUs.GameOptions;

using TOHE.Roles.Core;
using TOHE.Roles.Core.Interfaces;

namespace TOHE.Roles.Impostor;
public sealed class Minimalism : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        new(
            typeof(Minimalism),
            player => new Minimalism(player),
            CustomRoles.Minimalism,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            901635,
            SetupOptionItem,
            "km"
        );
    public Minimalism(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    static OptionItem KillCooldown;
    private static void SetupOptionItem()
    {
        KillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(2.5f, 180f, 2.5f), 10f, false)
            .SetValueFormat(OptionFormat.Seconds);
    }
    public float CalculateKillCooldown() => KillCooldown.GetFloat();
    public override bool CanUseAbilityButton() => false;
    public override bool CanSabotage(SystemTypes systemType) => false;
    public override bool OnCheckReportDeadBody(PlayerControl reporter, GameData.PlayerInfo target) => Is(reporter);
    public bool CanUseImpostorVentButton { get; } = false;
}