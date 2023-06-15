using AmongUs.GameOptions;
using System.Linq;
using UnityEngine;

using TOHE.Modules;
using TOHE.Roles.Core;
using TOHE.Roles.Core.Interfaces;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;
public sealed class Bomber : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        new(
            typeof(Bomber),
            player => new Bomber(player),
            CustomRoles.Bomber,
            () => RoleTypes.Shapeshifter,
            CustomRoleTypes.Impostor,
            3100,
            SetupOptionItem,
            "bb"
        );
    public Bomber(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    static OptionItem OptionRadius;
    bool Shapeshifting;
    enum OptionName
    {
        BomberRadius
    }
    private static void SetupOptionItem()
    {
        OptionRadius = FloatOptionItem.Create(RoleInfo, 10, OptionName.BomberRadius, new(0.5f, 10f, 0.5f), 2f, false)
            .SetValueFormat(OptionFormat.Multiplier);
    }
    public bool CanKill { get; private set; } = false;
    public float CalculateKillCooldown() => 255f;
    public override bool OverrideAbilityButtonText(out string text)
    {
        text = GetString("BomberShapeshiftText");
        return true;
    }
    public override void OnShapeshift(PlayerControl target)
    {
        Shapeshifting = !Is(target);

        if (!AmongUsClient.Instance.AmHost) return;

        if (!Shapeshifting) return;

        Logger.Info("炸弹爆炸了", "Boom");
        CustomSoundsManager.RPCPlayCustomSoundAll("Boom");
        foreach (var tg in Main.AllPlayerControls)
        {
            if (!tg.IsModClient()) tg.KillFlash();
            var pos = Player.transform.position;
            var dis = Vector2.Distance(pos, tg.transform.position);

            if (!tg.IsAlive() || tg.IsEaten()) continue;
            if (dis > OptionRadius.GetFloat()) continue;
            if (tg.PlayerId == Player.PlayerId) continue;

            var state = PlayerState.GetByPlayerId(tg.PlayerId);
            state.DeathReason = CustomDeathReason.Bombed;
            tg.SetRealKiller(Player);
            tg.RpcMurderPlayerV2(tg);
        }
        new LateTask(() =>
        {
            //自分が最後の生き残りの場合は勝利のために死なない
            if (Main.AllAlivePlayerControls.Count() > 1 && Player.IsAlive() && !GameStates.IsEnded)
            {
                var state = PlayerState.GetByPlayerId(Player.PlayerId);
                state.DeathReason = CustomDeathReason.Bombed;
                Player.RpcExileV2();
                state.SetDead();
            }
            Utils.NotifyRoles();
        }, 1.5f, "Bomber Suiscide");

    }
    public override void OnExileWrapUp(GameData.PlayerInfo exiled, ref bool DecidedWinner) => Player.RpcResetAbilityCooldown();
}