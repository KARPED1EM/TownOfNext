using AmongUs.GameOptions;
using Hazel;
using TOHE.Modules;
using TOHE.Roles.Core;
using TOHE.Roles.Core.Interfaces;
using UnityEngine;

namespace TOHE.Roles.Impostor;
public sealed class Escapee : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        new(
            typeof(Escapee),
            player => new Escapee(player),
            CustomRoles.Escapee,
            () => RoleTypes.Shapeshifter,
            CustomRoleTypes.Impostor,
            2000,
            null,
            "ec"
        );
    public Escapee(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        Marked = false;
    }

    private bool Shapeshifting;
    private bool Marked;
    private Vector2 MarkedPosition;
    public override void Add()
    {
        Marked = false;
        Shapeshifting = false;
    }
    private void SendRPC()
    {
        using var sender = CreateSender(CustomRPC.SyncEscapee);
        sender.Writer.Write(Marked);
    }
    public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        if (rpcType != CustomRPC.SyncEscapee) return;
        Marked = reader.ReadBoolean();
    }
    public override bool OverrideAbilityButtonText(out string text)
    {
        text = Marked ? Translator.GetString("EscapeeTeleportButtonText") : Translator.GetString("EscapeeMarkButtonText");
        return !Shapeshifting;
    }
    public override void OnShapeshift(PlayerControl target)
    {
        Shapeshifting = !Is(target);

        if (!AmongUsClient.Instance.AmHost) return;

        if (!Shapeshifting) return;

        if (Marked)
        {
            Marked = false;
            Player.RPCPlayCustomSound("Teleport");
            Utils.TP(Player.NetTransform, MarkedPosition);
            Logger.Msg($"{Player.GetNameWithRole()}：{MarkedPosition}", "Escapee.OnShapeshift");
        }
        else
        {
            MarkedPosition = Player.GetTruePosition();
            Marked = true;
            SendRPC();
        }
    }
}