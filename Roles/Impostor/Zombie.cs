using AmongUs.GameOptions;
using TOHE.Roles.Core;
using TOHE.Roles.Core.Interfaces;

namespace TOHE.Roles.Impostor;
public sealed class Zombie : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        new(
            typeof(Zombie),
            player => new Zombie(player),
            CustomRoles.Zombie,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            901790,
            SetupOptionItem,
            "zb",
            experimental: true
        );
    public Zombie(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    static OptionItem OptionKillCooldown;
    static OptionItem OptionSpeedReduce;
    enum OptionName
    {
        ZombieSpeedReduce
    }

    private static void SetupOptionItem()
    {
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(2.5f, 180f, 2.5f), 12f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionSpeedReduce = FloatOptionItem.Create(RoleInfo, 11, OptionName.ZombieSpeedReduce, new(0.01f, 1f, 0.01f), 0.03f, false)
            .SetValueFormat(OptionFormat.Multiplier);
    }
    public float CalculateKillCooldown()
    {
        Main.AllPlayerSpeed[Player.PlayerId] -= OptionSpeedReduce.GetFloat();
        return OptionKillCooldown.GetFloat();
    }
    public override void ApplyGameOptions(IGameOptions opt) => opt.SetFloat(FloatOptionNames.ImpostorLightMod, 0.2f);
    public override void OnCalculateVotes(ref PlayerVoteArea ps, ref int VoteNums)
    {
        if (CustomRoleManager.GetByPlayerId(ps.VotedFor) is Zombie) VoteNums = 0;
    }
}