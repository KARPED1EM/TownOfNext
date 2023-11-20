using HarmonyLib;
using Hazel;
using System;
using System.Collections.Generic;
using System.Linq;
using TONX.Roles.Core;

namespace TONX.Modules;

public class MessageControl
{
    public string Message { get; set; }
    public string Args { get; set; }
    public bool HasValidArgs { get; set; }

    public PlayerControl Player { get; set; }
    public bool IsAlive { get => Player.IsAlive(); }
    public bool IsFromMod { get => Player.IsModClient(); }
    public bool IsFromSelf { get => Player.AmOwner; }

    public bool IsCommand { get; set; } = false;
    public MsgRecallMode RecallMode { get; set; } = MsgRecallMode.None;

    public MessageControl(PlayerControl player, string message)
    {
        Player = player;
        Message = message;

        if (ChatCommand.AllCommands == null || !ChatCommand.AllCommands.Any())
            ChatCommand.Init();

        MsgRecallMode recallMode = MsgRecallMode.None;
        // Check if it is a role command
        IsCommand = Player.GetRoleClass()?.OnSendMessage(Message, out recallMode) ?? false;
        CustomRoleManager.ReceiveMessage.Do(a => a.Invoke(this));

        RecallMode = recallMode;
        if (IsCommand || !AmongUsClient.Instance.AmHost) return;

        if (!IsCommand && AmongUsClient.Instance.AmHost)
        {
            // Not a role command, check for command list
            foreach (var command in ChatCommand.AllCommands)
            {
                if (command.Access switch
                {
                    CommandAccess.All => false,
                    CommandAccess.LocalMod => !IsFromSelf,
                    CommandAccess.Host => !AmongUsClient.Instance.AmHost || !IsFromSelf,
                    CommandAccess.Debugger => !DebugModeManager.AmDebugger,
                    _ => true,
                }) continue;

                string keyword = command.KeyWords.Find(k => Message.ToLower().StartsWith("/" + k.ToLower()));
                if (string.IsNullOrEmpty(keyword)) continue;

                Args = Message[(keyword.Length + 1)..].Trim();
                HasValidArgs = !string.IsNullOrWhiteSpace(Args);

                Logger.Info($"Command: /{keyword}, Args: {Args}", "ChatControl");

                (RecallMode, string msg) = command.Command(this);
                if (!string.IsNullOrEmpty(msg)) Utils.SendMessage(msg, Player.PlayerId);
                IsCommand = true;
                return;
            }
        }
    }

    public static void Spam(bool includeHost = false)
    {
        if (!AmongUsClient.Instance.AmHost) return;

        List<CustomRoles> roles = Enum.GetValues(typeof(CustomRoles)).Cast<CustomRoles>().Where(x => x is not CustomRoles.NotAssigned).ToList();
        var rd = IRandom.Instance;
        string msg;
        string[] command = new string[] { "bet", "bt", "guess", "gs", "shoot", "st", "赌", "猜", "审判", "tl", "判", "审" };
        for (int i = 0; i < 20; i++)
        {
            msg = "/";
            if (rd.Next(1, 100) < 20)
            {
                msg += "id";
            }
            else
            {
                msg += command[rd.Next(0, command.Length - 1)];
                msg += rd.Next(1, 100) < 50 ? string.Empty : " ";
                msg += rd.Next(0, 15).ToString();
                msg += rd.Next(1, 100) < 50 ? string.Empty : " ";
                CustomRoles role = roles[rd.Next(0, roles.Count)];
                msg += rd.Next(1, 100) < 50 ? string.Empty : " ";
                msg += Utils.GetRoleName(role);
            }
            var player = Main.AllAlivePlayerControls.ToArray()[rd.Next(0, Main.AllAlivePlayerControls.Count())];
            if (includeHost) DestroyableSingleton<HudManager>.Instance.Chat.AddChat(player, msg);
            var writer = CustomRpcSender.Create("MessagesToSend", SendOption.None);
            writer.StartMessage(-1);
            writer.StartRpc(player.NetId, (byte)RpcCalls.SendChat)
                .Write(msg)
                .EndRpc();
            writer.EndMessage();
            writer.SendMessage();
        }
    }
}

public enum MsgRecallMode
{
    None,
    Block, // Cancel sending msg for modded client
    Spam, // Spam lot of messages to hide original message
}