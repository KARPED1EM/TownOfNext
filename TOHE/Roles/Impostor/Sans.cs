using AmongUs.GameOptions;
using System;

using TOHE.Roles.Core;
using TOHE.Roles.Core.Interfaces;

namespace TOHE.Roles.Impostor;
public sealed class Sans : RoleBase, IImpostor
{

    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Sans),
            player => new Sans(player),
            CustomRoles.Sans,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            3000,
            SetupOptionItem,
            "ag"
        );
    public Sans(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    static OptionItem DefaultKillCooldown;
    static OptionItem ReduceKillCooldown;
    static OptionItem MinKillCooldown;
    enum OptionName
    {
        SansDefaultKillCooldown,
        SansReduceKillCooldown,
        SansMinKillCooldown,
    }
    private float KillCooldown;
    private static void SetupOptionItem()
    {
        DefaultKillCooldown = FloatOptionItem.Create(RoleInfo, 10, OptionName.SansDefaultKillCooldown, new(2.5f, 180f, 2.5f), 65f, false)
            .SetValueFormat(OptionFormat.Seconds);
        ReduceKillCooldown = FloatOptionItem.Create(RoleInfo, 11, OptionName.SansReduceKillCooldown, new(2.5f, 180f, 2.5f), 15f, false)
            .SetValueFormat(OptionFormat.Seconds);
        MinKillCooldown = FloatOptionItem.Create(RoleInfo, 12, OptionName.SansMinKillCooldown, new(2.5f, 180f, 2.5f), 2.5f, false)
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Add()
    {
        KillCooldown = DefaultKillCooldown.GetFloat();
    }
    public float CalculateKillCooldown() => KillCooldown;

    public void BeforeMurderPlayerAsKiller(MurderInfo info)
    {
        if (info.IsSuicide) return;
        var killer = info.AttemptKiller;
        KillCooldown = Math.Clamp(KillCooldown - ReduceKillCooldown.GetFloat(), MinKillCooldown.GetFloat(), DefaultKillCooldown.GetFloat());
        killer.ResetKillCooldown();
        killer.SyncSettings();
    }
}