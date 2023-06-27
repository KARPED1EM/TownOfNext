using AmongUs.GameOptions;
using Hazel;
using InnerNet;
using System.Collections.Generic;
using System.Linq;
using TOHE.Roles.Core;
using TOHE.Roles.Core.Interfaces;
using UnityEngine;

namespace TOHE.Roles.Impostor;
public sealed class OverKiller : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(OverKiller),
            player => new OverKiller(player),
            CustomRoles.OverKiller,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            4700,
            null,
            "bu",
            experimental: true
        );
    public OverKiller(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    private List<byte> OverKillerKilledPlayers;
    public override void OnDestroy() => OverKillerKilledPlayers.Clear();
    public override void Add() => OverKillerKilledPlayers = new();
    public bool OverrideKillButtonText(out string text)
    {
        text = Translator.GetString("OverKillerButtonText");
        return true;
    }
    public void BeforeMurderPlayerAsKiller(MurderInfo info)
    {
        if (info.IsSuicide) return;
        info.DoKill = false;
        var (killer, target) = info.AttemptTuple;

        if (OverKillerKilledPlayers.Contains(target.PlayerId)) return;

        PlayerState.GetByPlayerId(target.PlayerId).DeathReason = CustomDeathReason.Dismembered;
        new LateTask(() =>
        {
            if (!OverKillerKilledPlayers.Contains(target.PlayerId)) OverKillerKilledPlayers.Add(target.PlayerId);
            var ops = target.GetTruePosition();
            var rd = IRandom.Instance;
            for (int i = 0; i < 20; i++)
            {
                Vector2 location = new(ops.x + ((float)(rd.Next(0, 201) - 100) / 100), ops.y + ((float)(rd.Next(0, 201) - 100) / 100));
                location += new Vector2(0, 0.3636f);

                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(target.NetTransform.NetId, (byte)RpcCalls.SnapTo, SendOption.None, -1);
                NetHelpers.WriteVector2(location, writer);
                writer.Write(target.NetTransform.lastSequenceId);
                AmongUsClient.Instance.FinishRpcImmediately(writer);

                target.NetTransform.SnapTo(location);
                killer.MurderPlayer(target);

                if (target.Is(CustomRoles.Avanger))
                {
                    var pcList = Main.AllAlivePlayerControls.Where(x => x.PlayerId != target.PlayerId).ToList();
                    var rp = pcList[IRandom.Instance.Next(0, pcList.Count)];
                    var state = PlayerState.GetByPlayerId(rp.PlayerId).DeathReason = CustomDeathReason.Revenge;
                    rp.SetRealKiller(target);
                    rp.RpcMurderPlayerV2(rp);
                }

                MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(killer.NetId, (byte)RpcCalls.MurderPlayer, SendOption.None, -1);
                messageWriter.WriteNetObject(target);
                AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
            }
            Utils.TP(killer.NetTransform, ops);
        }, 0.05f, "OverKiller Murder");

        return;
    }
}