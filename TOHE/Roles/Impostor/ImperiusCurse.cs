using AmongUs.GameOptions;

using TOHE.Roles.Core;
using TOHE.Roles.Core.Interfaces;

namespace TOHE.Roles.Impostor;
public sealed class ImperiusCurse : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(ImperiusCurse),
            player => new ImperiusCurse(player),
            CustomRoles.ImperiusCurse,
            () => RoleTypes.Shapeshifter,
            CustomRoleTypes.Impostor,
            3600,
            SetupOptionItem,
            "st|奪魂者|多混|夺魂",
            experimental: true
        );
    public ImperiusCurse(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    static OptionItem OptionShapeshiftCooldown;
    static OptionItem OptionShapeshiftDuration;

    private static void SetupOptionItem()
    {
        OptionShapeshiftCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.ShapeshiftCooldown, new(2.5f, 180f, 2.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionShapeshiftDuration = FloatOptionItem.Create(RoleInfo, 11, GeneralOption.ShapeshiftDuration, new(2.5f, 180f, 2.5f), 15f, false)
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.ShapeshifterLeaveSkin = false;
        AURoleOptions.ShapeshifterCooldown = OptionShapeshiftCooldown.GetFloat();
        AURoleOptions.ShapeshifterDuration = OptionShapeshiftDuration.GetFloat();
    }
    public override bool OverrideAbilityButtonText(out string text)
    {
        text = Translator.GetString("ImperiusCurseButtonText");
        return !Shapeshifting;
    }
    private bool Shapeshifting = false;
    public override void OnShapeshift(PlayerControl target)
    {
        Shapeshifting = !Is(target);

        if (!AmongUsClient.Instance.AmHost) return;

        if (Shapeshifting)
        {
            new LateTask(() =>
            {
                if (!(!GameStates.IsInTask || !Player.IsAlive() || !target.IsAlive() || Player.inVent || target.inVent))
                {
                    var originPs = target.GetTruePosition();
                    Utils.TP(target.NetTransform, Player.GetTruePosition());
                    Utils.TP(Player.NetTransform, originPs);
                }
            }, 1.5f, "ImperiusCurse.OnShapeshift");
        }
    }
}