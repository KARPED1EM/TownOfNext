using AmongUs.GameOptions;
using Hazel;
using TOHE.Roles.Core;
using TOHE.Roles.Core.Interfaces;
using UnityEngine;

namespace TOHE.Roles.Crewmate;
public sealed class SwordsMan : RoleBase, IKiller
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(SwordsMan),
            player => new SwordsMan(player),
            CustomRoles.SwordsMan,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Crewmate,
            21400,
            null,
            "vi",
            "#f0e68c",
            true,
            broken: true
        );
    public SwordsMan(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False
    )
    { }

    private bool IsKilled;
    public override void Add()
    {
        var playerId = Player.PlayerId;
        IsKilled = false;

        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    private void SendRPC()
    {
        using var sender = CreateSender(CustomRPC.SwordsManKill);
        sender.Writer.Write(IsKilled);
    }
    public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        if (rpcType != CustomRPC.SwordsManKill) return;
        IsKilled = reader.ReadBoolean();
    }
    public float CalculateKillCooldown() => CanUseKillButton() ? 0f : 255f;
    public bool CanUseKillButton() => Player.IsAlive() && !IsKilled;
    public override bool CanSabotage(SystemTypes systemType) => false;
    public override void ApplyGameOptions(IGameOptions opt) => opt.SetVision(false);
    public bool OnCheckMurderAsKiller(MurderInfo info)
    {
        if (Is(info.AttemptKiller) && !info.IsSuicide)
        {
            if (IsKilled) return false;
            IsKilled = true;
            SendRPC();
            Player.ResetKillCooldown();
        }
        return true;
    }
    public override string GetProgressText(bool comms = false) => Utils.ColorString(CanUseKillButton() ? Color.yellow : Color.gray, $"({(CanUseKillButton() ? 1 : 0)})");
}