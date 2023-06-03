using AmongUs.GameOptions;
using Hazel;
using System.Collections.Generic;
using UnityEngine;

using TOHE.Roles.Core;
using TOHE.Roles.Core.Interfaces;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;
public sealed class Eraser : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        new(
            typeof(Eraser),
            player => new Eraser(player),
            CustomRoles.Eraser,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            905553,
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
    static OptionItem OptionHideVote;
    enum OptionName
    {
        EraseLimit,
        EraserHideVote,
    }

    private int EraseLimit;
    private byte PlayerToErase;
    private static void SetupOptionItem()
    {
        OptionEraseLimit = IntegerOptionItem.Create(RoleInfo, 10, OptionName.EraseLimit, new(1, 15, 1), 2, false)
            .SetValueFormat(OptionFormat.Times);
        OptionHideVote = BooleanOptionItem.Create(RoleInfo, 11, OptionName.EraserHideVote, false, false);
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
    public override bool OnCheckForEndVoting(ref List<MeetingHud.VoterState> statesList, PlayerVoteArea pva)
    {
        var target = Utils.GetPlayerById(pva.VotedFor);
        if (target != null && pva.DidVote && pva.VotedFor < 253 && Player.IsAlive() && PlayerToErase != byte.MaxValue && EraseLimit >= 1)
        {
            if (Is(target))
            {
                Utils.SendMessage(GetString("EraserEraseSelf"), Player.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Eraser), GetString("EraserEraseMsgTitle")));
                return true;
            }

            if (target.Is(CustomRoleTypes.Neutral))
            {
                Utils.SendMessage(string.Format(GetString("EraserEraseNeutralNotice"), target.GetRealName()), Player.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Eraser), GetString("EraserEraseMsgTitle")));
                return true;
            }

            EraseLimit--;
            SendRPC();

            PlayerToErase = target.PlayerId;

            Utils.SendMessage(string.Format(GetString("EraserEraseNotice"), target.GetRealName()), Player.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Eraser), GetString("EraserEraseMsgTitle")));
        }
        return true;
    }
    public override bool OnVotingEnd(ref List<MeetingHud.VoterState> statesList, ref PlayerVoteArea pva) => !OptionHideVote.GetBool();
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
