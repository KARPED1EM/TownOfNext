using HarmonyLib;
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

            string keyword = command.KeyWords.Find(k => Message.StartsWith("/" + k));
            if (string.IsNullOrEmpty(keyword)) continue;

            Args = Message[(keyword.Length + 1)..].Trim();
            HasValidArgs = !string.IsNullOrWhiteSpace(Args);

            Logger.Info($"Command: /{keyword}, Args: {Args}", "ChatControl");

            (RecallMode, string msg) = command.Command(this);
            if (!string.IsNullOrEmpty(msg)) Utils.SendMessage(msg, Player.PlayerId);
            IsCommand = true;
            return;
        }

        MsgRecallMode recallMode = MsgRecallMode.None;
        Player.GetRoleClass()?.OnSendMessage(Message, out recallMode);
        CustomRoleManager.ReceiveMessage.Do(a => a.Invoke(this));

        IsCommand = recallMode != MsgRecallMode.None;
        RecallMode = recallMode;
    }

    public static void Spam()
    {
        return;
    }
}

public enum MsgRecallMode
{
    None,
    Block, // Cancel sending msg for modded client
    Spam, // Spam lot of messages to hide original message
}