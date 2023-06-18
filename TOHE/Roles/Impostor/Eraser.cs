using AmongUs.GameOptions;
using Hazel;
using System.Collections.Generic;
using TOHE.Roles.Core;
using TOHE.Roles.Core.Interfaces;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;
public sealed class Eraser : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Eraser),
            player => new Eraser(player),
            CustomRoles.Eraser,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            4600,
            SetupOptionItem,
            "er",
            experimental: true
        );
    public Eraser(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    static OptionItem OptionEraseLimit;
    static OptionItem OptionIgnoreVote;
    enum OptionName
    {
        EraseLimit,
        EraserIgnoreVote,
    }

    private int EraseLimit;
    private byte PlayerToErase;
    private static void SetupOptionItem()
    {
        OptionEraseLimit = IntegerOptionItem.Create(RoleInfo, 10, OptionName.EraseLimit, new(1, 15, 1), 2, false)
            .SetValueFormat(OptionFormat.Times);
        OptionIgnoreVote = BooleanOptionItem.Create(RoleInfo, 11, OptionName.EraserIgnoreVote, false, false);
    }
    public override void Add()
    {
        EraseLimit = OptionEraseLimit.GetInt();
        PlayerToErase = byte.MaxValue;
    }
    private void SendRPC()
    {
        using var sender = CreateSender(CustomRPC.SetEraseLimit);
        sender.Writer.Write(EraseLimit);
    }
    public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        if (rpcType != CustomRPC.SetEraseLimit) return;
        EraseLimit = reader.ReadInt32();
    }
    public override string GetProgressText(bool comms = false) => Utils.ColorString(EraseLimit >= 1 ? Color.red : Color.gray, $"({EraseLimit})");
    public override (byte? votedForId, int? numVotes, bool doVote) OnVote(byte voterId, byte sourceVotedForId)
    {
        var (votedForId, numVotes, doVote) = base.OnVote(voterId, sourceVotedForId);
        var baseVote = (votedForId, numVotes, doVote);
        var target = Utils.GetPlayerById(sourceVotedForId);
        if (target == null || voterId != Player.PlayerId || sourceVotedForId >= 253 || !Player.IsAlive() || EraseLimit < 1)
        {
            return baseVote;
        }
        if (OptionIgnoreVote.GetBool())
        {
            baseVote.doVote = false;
        }

        if (Is(target))
        {
            Utils.SendMessage(GetString("EraserEraseSelf"), Player.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Eraser), GetString("EraserEraseMsgTitle")));
            return baseVote;
        }

        if (target.Is(CustomRoleTypes.Neutral))
        {
            Utils.SendMessage(string.Format(GetString("EraserEraseNeutralNotice"), target.GetRealName()), Player.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Eraser), GetString("EraserEraseMsgTitle")));
            return baseVote;
        }

        EraseLimit--;
        SendRPC();

        PlayerToErase = target.PlayerId;

        Utils.SendMessage(string.Format(GetString("EraserEraseNotice"), target.GetRealName()), Player.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Eraser), GetString("EraserEraseMsgTitle")));

        return baseVote;
    }
    public override void OnStartMeeting()
    {
        PlayerToErase = byte.MaxValue;
    }
    public override void AfterMeetingTasks()
    {
        if (PlayerToErase == byte.MaxValue) return;
        var player = Utils.GetPlayerById(PlayerToErase);
        if (player == null) return;
        player.RpcSetCustomRole(player.GetCustomRole().GetRoleInfo().BaseRoleType.Invoke().GetCustomRoleTypes());
        NameNotifyManager.Notify(player, GetString("LostRoleByEraser"));
        Logger.Info($"{player.GetNameWithRole()} 被擦除了", "Eraser.AfterMeetingTasks");

    }
}
