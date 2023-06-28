using AmongUs.GameOptions;
using Hazel;
using TONX.Roles.Core;
using TONX.Roles.Core.Interfaces;
using UnityEngine;
using static TONX.Translator;

namespace TONX.Roles.Impostor;
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
            "er|抹除者|抹除",
            experimental: true
        );
    public Eraser(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    static OptionItem OptionEraseLimit;
    enum OptionName
    {
        EraseLimit
    }

    private int EraseLimit;
    private byte PlayerToErase;
    private static void SetupOptionItem()
    {
        OptionEraseLimit = IntegerOptionItem.Create(RoleInfo, 10, OptionName.EraseLimit, new(1, 15, 1), 2, false)
            .SetValueFormat(OptionFormat.Times);
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
    public override bool OnVote(byte voterId, byte sourceVotedForId, ref byte roleVoteFor, ref int roleNumVotes, ref bool clearVote)
    {
        var target = Utils.GetPlayerById(sourceVotedForId);
        if (target == null || voterId != Player.PlayerId || sourceVotedForId >= 253 || !Player.IsAlive() || EraseLimit < 1 || PlayerToErase != byte.MaxValue) return true;

        if (Is(target))
        {
            string notice1 = GetString("EraserEraseSelf") + GetString("SkillDoneAndYouCanVoteNormallyNow");
            Player.ShowPopUp(notice1);
            Utils.SendMessage(notice1, Player.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Eraser), GetString("EraserEraseMsgTitle")));
            clearVote = true;
            return false;
        }

        if (target.Is(CustomRoleTypes.Neutral))
        {
            string notice2 = string.Format(GetString("EraserEraseNeutralNotice"), target.GetRealName()) + GetString("SkillDoneAndYouCanVoteNormallyNow");
            Player.ShowPopUp(notice2);
            Utils.SendMessage(notice2, Player.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Eraser), GetString("EraserEraseMsgTitle")));
            clearVote = true;
            return false;
        }

        EraseLimit--;
        SendRPC();

        PlayerToErase = target.PlayerId;

        string notice3 = string.Format(GetString("EraserEraseNotice"), target.GetRealName()) + GetString("SkillDoneAndYouCanVoteNormallyNow");
        Player.ShowPopUp(notice3);
        Utils.SendMessage(notice3, Player.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Eraser), GetString("EraserEraseMsgTitle")));

        clearVote = true;
        return false;
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
