using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Assets.CoreScripts;
using HarmonyLib;
using Hazel;
using Il2CppSystem.Runtime.Remoting.Messaging;
using UnityEngine;
using static TownOfHost.Translator;

namespace TownOfHost
{
    [HarmonyPatch(typeof(ChatController), nameof(ChatController.SendChat))]
    class ChatCommands
    {
        [DllImport("kernel32.dll", EntryPoint = "LCMapStringA")]
        public static extern int LCMapString(int Locale, int dwMapFlags, byte[] lpSrcStr, int cchSrc, byte[] lpDestStr, int cchDest);
        const int LCMAP_SIMPLIFIED_CHINESE = 0x02000000;
        const int LCMAP_TRADITIONAL_CHINESE = 0x04000000;

        //转化方法
        public static string ToTraditional(string source, int type)
        {
            byte[] srcByte2 = Encoding.Default.GetBytes(source);
            byte[] desByte2 = new byte[srcByte2.Length];
            LCMapString(2052, type, srcByte2, -1, desByte2, srcByte2.Length);
            string des2 = Encoding.Default.GetString(desByte2);
            return des2;
        }

        public static List<string> ChatHistory = new();

        public static bool ContainsStart(string text)
        {
            if (text == "Start") return true;
            if (text == "start") return true;
            if (text == "开") return true;
            if (text == "开始") return true;
            if (text == "开啊") return true;
            if (text == "开阿") return true;
            if (text == "kai") return true;
            if (text == "KAI") return true;
            if (text.Contains("started")) return false;
            if (text.Contains("starter")) return false;
            if (text.Contains("Starting")) return false;
            if (text.Contains("starting")) return false;
            if (text.Contains("beginner")) return false;
            if (text.Contains("beginned")) return false;
            if (text.Contains("了")) return false;
            if (text.Contains("吗")) return false;
            if (text.Contains("吧")) return false;
            if (text.Contains("哈")) return false;
            if (text.Contains("还")) return false;
            if (text.Contains("现")) return false;
            if (text.Contains("不")) return false;
            if (text.Contains("可")) return false;
            if (text.Contains("?")) return false;
            if (text.Contains("？")) return false;
            if (text.Length >= 6) return false;
            if (text.Contains("start")) return true;
            if (text.Contains("s t a r t")) return true;
            if (text.Contains("begin")) return true;
            if (text.Contains("开")) return true;
            if (text.Contains("kai")) return true;
            if (text.Contains("KAI")) return true;
            return false;
        }

        public static bool ProhibitedCheck(PlayerControl player, string text)
        {

            string name = player.GetRealName();

            if (Options.AutoKickStart.GetBool())
            {
                if (ContainsStart(text) && GameStates.IsLobby && player.PlayerId != PlayerControl.LocalPlayer.PlayerId)
                {
                    Utils.SendMessage($"{name} 被请离\n请不要催促开始，很抱歉这是设定");
                    AmongUsClient.Instance.KickPlayer(player.GetClientId(), false);
                    Logger.Msg($"{name} 因催促开始被请离", "Blocked Word");
                    return true;
                }
            }

            if (!Options.AutoKickStopWords.GetBool() && !Options.AutoWarnStopWords.GetBool()) return false;

            var list = ReturnAllNewLinesInFile(Main.BANNEDWORDS_FILE_PATH, noErr: true);
            bool banned = false;
            var banedWord = "";

            foreach (var word in list)
            {
                if (word != null && text.Contains(word))
                {
                    banedWord = word;
                    banned = true;
                    break;
                }
            }

            if (banned && player.PlayerId != PlayerControl.LocalPlayer.PlayerId)
            {
                if (Options.AutoWarnStopWords.GetBool()) Utils.SendMessage($"{name} ，请友善讨论，杜绝脏话哦~");
                Logger.Msg($"{name} 触发违禁词： {banedWord}.", "Blocked Word");

                if (!Options.AutoKickStopWords.GetBool()) return true;
                Utils.SendMessage($"{name} 被踢出因其触发了违禁词");
                AmongUsClient.Instance.KickPlayer(player.GetClientId(), false);
                Logger.Msg($"{name} said a word blocked by this host. The blocked word was {banedWord}.", "Blocked Word");
                return true;
            }
            return false;
        }

        public static bool Prefix(ChatController __instance)
        {
            if (__instance.TextArea.text == "") return false;
            __instance.TimeSinceLastMessage = 3f;
            var text = __instance.TextArea.text;
            if (ProhibitedCheck(PlayerControl.LocalPlayer, text)) return false;
            if (ChatHistory.Count == 0 || ChatHistory[^1] != text) ChatHistory.Add(text);
            ChatControllerUpdatePatch.CurrentHistorySelection = ChatHistory.Count;
            string[] args = text.Split(' ');
            string subArgs = "";
            var canceled = false;
            var cancelVal = "";
            Main.isChatCommand = true;
            Logger.Info(text, "SendChat");
            switch (args[0])
            {
                case "/dump":
                    canceled = true;
                    Utils.DumpLog();
                    break;
                case "/v":
                case "/version":
                    canceled = true;
                    string version_text = "";
                    foreach (var kvp in Main.playerVersion.OrderBy(pair => pair.Key))
                    {
                        version_text += $"{kvp.Key}:{Utils.GetPlayerById(kvp.Key)?.Data?.PlayerName}:{kvp.Value.forkId}/{kvp.Value.version}({kvp.Value.tag})\n";
                    }
                    if (version_text != "") HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, version_text);
                    break;
                default:
                    Main.isChatCommand = false;
                    break;
            }
            if (AmongUsClient.Instance.AmHost)
            {
                Main.isChatCommand = true;
                switch (args[0])
                {
                    case "/win":
                    case "/winner":
                        canceled = true;
                        Utils.SendMessage("Winner: " + string.Join(",", Main.winnerList.Select(b => Main.AllPlayerNames[b])));
                        break;

                    case "/l":
                    case "/lastresult":
                        canceled = true;
                        Utils.ShowLastResult();
                        break;

                    case "/rn":
                    case "/rename":
                        canceled = true;
                        Main.nickName = args.Length > 1 ? Main.nickName = args[1] : "";
                        break;

                    case "/hn":
                    case "/hidename":
                        canceled = true;
                        Main.HideName.Value = args.Length > 1 ? args.Skip(1).Join(delimiter: " ") : Main.HideName.DefaultValue.ToString();
                        GameStartManagerPatch.GameStartManagerStartPatch.HideName.text =
                            ColorUtility.TryParseHtmlString(Main.HideColor.Value, out _)
                                ? $"<color={Main.HideColor.Value}>{Main.HideName.Value}</color>"
                                : $"<color={Main.ModColor}>{Main.HideName.Value}</color>";
                        break;

                    case "/level":
                        canceled = true;
                        subArgs = args.Length < 2 ? "" : args[1];
                        Utils.SendMessage("您的等级设置为：" + subArgs, PlayerControl.LocalPlayer.PlayerId);
                        //nt32.Parse("-105");
                        var number = System.Convert.ToUInt32(subArgs);
                        PlayerControl.LocalPlayer.RpcSetLevel(number - 1);
                        break;

                    case "/n":
                    case "/now":
                        canceled = true;

                        Utils.SendMessage(GetString("Message.NowOverrideText"), PlayerControl.LocalPlayer.PlayerId);
                        //TemplateManager.SendTemplate("now", noErr: true);
                        break;

                        subArgs = args.Length < 2 ? "" : args[1];
                        switch (subArgs)
                        {
                            case "r":
                            case "roles":
                                Utils.ShowActiveRoles();
                                break;
                            default:
                                Utils.ShowActiveSettings();
                                break;
                        }
                        break;

                    case "/dis":
                        canceled = true;
                        subArgs = args.Length < 2 ? "" : args[1];
                        switch (subArgs)
                        {
                            case "crew":
                                GameManager.Instance.enabled = false;
                                GameManager.Instance.RpcEndGame(GameOverReason.HumansDisconnect, false);
                                break;

                            case "imp":
                                GameManager.Instance.enabled = false;
                                GameManager.Instance.RpcEndGame(GameOverReason.ImpostorDisconnect, false);
                                break;

                            default:
                                __instance.AddChat(PlayerControl.LocalPlayer, "crew | imp");
                                cancelVal = "/dis";
                                break;
                        }
                        ShipStatus.Instance.RpcRepairSystem(SystemTypes.Admin, 0);
                        break;

                    case "/r":
                    case "/roles":
                        canceled = true;
                        subArgs = args.Length < 2 ? "" : args[1];
                        GetRolesInfo(subArgs, PlayerControl.LocalPlayer);
                        break;

                    case "/h":
                    case "/help":
                        canceled = true;
                        subArgs = args.Length < 2 ? "" : args[1];
                        switch (subArgs)
                        {
                            
                            case "a":
                            case "addons":
                                subArgs = args.Length < 3 ? "" : args[2];
                                switch (subArgs)
                                {
                                    case "lastimpostor":
                                    case "limp":
                                        Utils.SendMessage(Utils.GetRoleName(CustomRoles.LastImpostor) + GetString("LastImpostorInfoLong"));
                                        break;

                                    default:
                                        Utils.SendMessage($"{GetString("Command.h_args")}:\n lastimpostor(limp)");
                                        break;
                                }
                                break;

                            case "m":
                            case "modes":
                                subArgs = args.Length < 3 ? "" : args[2];
                                switch (subArgs)
                                {
                                    case "hideandseek":
                                    case "has":
                                        Utils.SendMessage(GetString("HideAndSeekInfo"));
                                        break;

                                    case "nogameend":
                                    case "nge":
                                        Utils.SendMessage(GetString("NoGameEndInfo"));
                                        break;

                                    case "syncbuttonmode":
                                    case "sbm":
                                        Utils.SendMessage(GetString("SyncButtonModeInfo"));
                                        break;

                                    case "randommapsmode":
                                    case "rmm":
                                        Utils.SendMessage(GetString("RandomMapsModeInfo"));
                                        break;

                                    default:
                                        Utils.SendMessage($"{GetString("Command.h_args")}:\n hideandseek(has), nogameend(nge), syncbuttonmode(sbm), randommapsmode(rmm)");
                                        break;
                                }
                                break;


                            case "n":
                            case "now":
                                Utils.ShowActiveSettingsHelp();
                                break;

                            default:
                                Utils.ShowHelp();
                                break;
                        }
                        break;

                    case "/m":
                    case "/myrole":
                        canceled = true;
                        var role = PlayerControl.LocalPlayer.GetCustomRole();
                        if (GameStates.IsInGame)
                            HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, GetString(role.ToString()) + PlayerControl.LocalPlayer.GetRoleInfo(true));
                        break;

                    case "/t":
                    case "/template":
                        canceled = true;
                        if (args.Length > 1) TemplateManager.SendTemplate(args[1]);
                        else HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, $"{GetString("ForExample")}:\n{args[0]} test");
                        break;

                    case "/mw":
                    case "/messagewait":
                        canceled = true;
                        if (args.Length > 1 && int.TryParse(args[1], out int sec))
                        {
                            Main.MessageWait.Value = sec;
                            Utils.SendMessage(string.Format(GetString("Message.SetToSeconds"), sec), 0);
                        }
                        else Utils.SendMessage($"{GetString("Message.MessageWaitHelp")}\n{GetString("ForExample")}:\n{args[0]} 3", 0);
                        break;

                    case "/say":
                    case "/s":
                        canceled = true;
                        if (args.Length > 1)
                            Utils.SendMessage(args.Skip(1).Join(delimiter: " "), title: $"<color=#ff0000>{GetString("MessageFromTheHost")}</color>");
                        break;

                    case "/exe":
                        canceled = true;
                        if (args.Length < 2 || !int.TryParse(args[1], out int id)) break;
                        Utils.GetPlayerById(id)?.RpcExileV2();
                        break;

                    case "/kill":
                        canceled = true;
                        if (args.Length < 2 || !int.TryParse(args[1], out int id2)) break;
                        Utils.GetPlayerById(id2)?.RpcMurderPlayer(Utils.GetPlayerById(id2));
                        break;

                    default:
                        Main.isChatCommand = false;
                        break;
                }
            }
            if (canceled)
            {
                Logger.Info("Command Canceled", "ChatCommand");
                __instance.TextArea.Clear();
                __instance.TextArea.SetText(cancelVal);
                __instance.quickChatMenu.ResetGlyphs();
            }
            return !canceled;
        }

        public static void GetRolesInfo(string role, PlayerControl player)
        {
            var roleList = new Dictionary<CustomRoles, string>
            {
                //GM
                { CustomRoles.GM, "管理员" },
                //Impostor役職
                { (CustomRoles)(-1), $"== {GetString("Impostor")} ==" }, //区切り用
                { CustomRoles.BountyHunter, "赏金猎人" },
                { CustomRoles.EvilTracker,"邪恶的追踪者" },
                { CustomRoles.FireWorks, "烟花商人" },
                { CustomRoles.Mare, "梦魇" },
                { CustomRoles.Mafia, "黑手党" },
                { CustomRoles.SerialKiller, "嗜血杀手" },
                //{ CustomRoles.ShapeMaster, "sha" },
                { CustomRoles.TimeThief, "蚀时者"},
                { CustomRoles.Sniper, "狙击手" },
                { CustomRoles.Puppeteer, "傀儡师" },
                { CustomRoles.Vampire, "吸血鬼" },
                { CustomRoles.Warlock, "术士" },
                { CustomRoles.Witch, "女巫" },
                //Madmate役職
                { (CustomRoles)(-2), $"== {GetString("Madmate")} ==" }, //区切り用
                { CustomRoles.MadGuardian, "背叛的守卫" },
                { CustomRoles.Madmate, "叛徒" },
                { CustomRoles.MadSnitch, "背叛的告密者" },
                { CustomRoles.SKMadmate, "叛徒跟班" },
                //両陣営役職
                { (CustomRoles)(-3), $"== {GetString("Impostor")} or {GetString("Crewmate")} ==" }, //区切り用
                { CustomRoles.Watcher, "窥视者" },
                //Crewmate役職
                { (CustomRoles)(-4), $"== {GetString("Crewmate")} ==" }, //区切り用
                { CustomRoles.Bait, "诱饵" },
                { CustomRoles.Dictator, "独裁者" },
                { CustomRoles.Doctor, "医生" },
                { CustomRoles.Lighter, "执灯人" },
                { CustomRoles.Mayor, "市长" },
                { CustomRoles.SabotageMaster, "修理大师" },
                { CustomRoles.Seer,"灵媒" },
                { CustomRoles.Sheriff, "警长" },
                { CustomRoles.Snitch, "告密者" },
                { CustomRoles.SpeedBooster, "增速者" },
                { CustomRoles.Trapper, "陷阱师" },
                //Neutral役職
                { (CustomRoles)(-5), $"== {GetString("Neutral")} ==" }, //区切り用
                { CustomRoles.Arsonist, "纵火犯" },
                { CustomRoles.Egoist, "野心家" },
                { CustomRoles.Executioner, "处刑人" },
                { CustomRoles.Jester, "小丑" },
                { CustomRoles.Opportunist, "投机者" },
                { CustomRoles.SchrodingerCat, "薛定谔的猫" },
                { CustomRoles.Terrorist, "恐怖分子" },
                { CustomRoles.Jackal, "豺狼" },
                //属性
                { (CustomRoles)(-6), $"== {GetString("Addons")} ==" }, //区切り用
                {CustomRoles.Lovers, "恋人" },
                //HAS
                { (CustomRoles)(-7), $"== {GetString("HideAndSeek")} ==" }, //区切り用
                { CustomRoles.HASFox, "狐狸" },
                { CustomRoles.HASTroll, "巨魔" },

            };

            role = role.Trim();
            if (role == "" || role == String.Empty)
            {
                Utils.SendMessage("指令格式：/r [职业]\n例如：/r 灵媒", player.PlayerId);
                return;
            }
            role = ToTraditional(role, LCMAP_SIMPLIFIED_CHINESE);   //繁体转简体

            var msg = "";
            var rolemsg = $"{GetString("Command.h_args")}";
            foreach (var r in roleList)
            {
                var roleName = r.Key.ToString();
                var roleShort = r.Value;

                if (String.Compare(role, roleName, true) == 0 || String.Compare(role, roleShort, true) == 0)
                {
                    Utils.SendMessage(GetString(roleName) + GetString($"{roleName}InfoLong"), player.PlayerId);
                    return;
                }

                var roleText = $"{roleName.ToLower()}({roleShort.ToLower()}), ";
                if ((int)r.Key < 0)
                {
                    msg += rolemsg + "\n" + roleShort + "\n";
                    rolemsg = "";
                }
                else if ((rolemsg.Length + roleText.Length) > 40)
                {
                    msg += rolemsg + "\n";
                    rolemsg = roleText;
                }
                else
                {
                    rolemsg += roleText;
                }
            }

            Utils.SendMessage("请正确拼写您要查询的职业哦~", player.PlayerId);
            return;

            msg += rolemsg;
            Utils.SendMessage(msg);
        }
        public static void OnReceiveChat(PlayerControl player, string text)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (ProhibitedCheck(player, text)) return;
            string[] args = text.Split(' ');
            string subArgs = "";
            switch (args[0])
            {
                case "/l":
                case "/lastresult":
                    Utils.ShowLastResult(player.PlayerId);
                    break;

                case "/n":
                case "/now":

                    Utils.SendMessage(GetString("Message.NowOverrideText"), player.PlayerId);
                    //TemplateManager.SendTemplate("now", noErr: true);
                    break;

                    subArgs = args.Length < 2 ? "" : args[1];
                    switch (subArgs)
                    {

                        case "r":
                        case "roles":
                            Utils.ShowActiveRoles(player.PlayerId);
                            break;

                        default:
                            Utils.ShowActiveSettings(player.PlayerId);
                            break;
                    }
                    break;

                case "/r":
                case "/roles":
                    subArgs = args.Length < 2 ? "" : args[1];
                    GetRolesInfo(subArgs, player);
                    break;

                case "/h":
                case "/help":
                    subArgs = args.Length < 2 ? "" : args[1];
                    switch (subArgs)
                    {
                        case "n":
                        case "now":
                            Utils.ShowActiveSettingsHelp(player.PlayerId);
                            break;
                    }
                    break;

                case "/m":
                case "/myrole":
                    var role = player.GetCustomRole();
                    if (GameStates.IsInGame)
                        Utils.SendMessage(GetString(role.ToString()) + player.GetRoleInfo(true), player.PlayerId);
                    break;

                case "/t":
                case "/template":
                    if (args.Length > 1) TemplateManager.SendTemplate(args[1], player.PlayerId);
                    else Utils.SendMessage($"{GetString("ForExample")}:\n{args[0]} test", player.PlayerId);
                    break;

                default:
                    break;
            }
        }

        public static List<string> ReturnAllNewLinesInFile(string filename, byte playerId = 0xff, bool noErr = false)
        {
            // Logger.Info($"Checking lines in directory {filename}.", "ReturnAllNewLinesInFile (ChatCommands)");
            if (!File.Exists(filename))
            {
                HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, $"No {filename} file found.");
                File.WriteAllText(filename, "Enter the desired stuff here.");
                return new List<string>();
            }
            using StreamReader sr = new(filename, Encoding.GetEncoding("UTF-8"));
            string text;
            string[] tmp = { };
            List<string> sendList = new();
            HashSet<string> tags = new();
            while ((text = sr.ReadLine()) != null)
            {
                if (text.Length > 1 && text != "")
                {
                    tags.Add(text.ToLower());
                    sendList.Add(text.Join(delimiter: "").Replace("\\n", "\n").ToLower());
                }
            }
            if (sendList.Count == 0 && !noErr)
            {
                if (playerId == 0xff)
                    HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, string.Format(GetString("Message.TemplateNotFoundHost"), Main.BANNEDWORDS_FILE_PATH, tags.Join(delimiter: ", ")));
                else Utils.SendMessage(string.Format(GetString("Message.TemplateNotFoundClient"), Main.BANNEDWORDS_FILE_PATH), playerId);
                return new List<string>();
            }
            else
            {
                return sendList;
            }
        }
    }
    [HarmonyPatch(typeof(ChatController), nameof(ChatController.Update))]
    class ChatUpdatePatch
    {
        public static bool DoBlockChat = false;
        public static void Postfix(ChatController __instance)
        {
            if (!AmongUsClient.Instance.AmHost || Main.MessagesToSend.Count < 1 || (Main.MessagesToSend[0].Item2 == byte.MaxValue && Main.MessageWait.Value > __instance.TimeSinceLastMessage)) return;
            if (DoBlockChat) return;
            var player = Main.AllAlivePlayerControls.OrderBy(x => x.PlayerId).FirstOrDefault();
            if (player == null) return;
            (string msg, byte sendTo, string title) = Main.MessagesToSend[0];
            Main.MessagesToSend.RemoveAt(0);
            int clientId = sendTo == byte.MaxValue ? -1 : Utils.GetPlayerById(sendTo).GetClientId();
            var name = player.Data.PlayerName;
            if (clientId == -1)
            {
                player.SetName(title);
                DestroyableSingleton<HudManager>.Instance.Chat.AddChat(player, msg);
                player.SetName(name);
            }
            var writer = CustomRpcSender.Create("MessagesToSend", SendOption.None);
            writer.StartMessage(clientId);
            writer.StartRpc(player.NetId, (byte)RpcCalls.SetName)
                .Write(title)
                .EndRpc();
            writer.StartRpc(player.NetId, (byte)RpcCalls.SendChat)
                .Write(msg)
                .EndRpc();
            writer.StartRpc(player.NetId, (byte)RpcCalls.SetName)
                .Write(player.Data.PlayerName)
                .EndRpc();
            writer.EndMessage();
            writer.SendMessage();
            __instance.TimeSinceLastMessage = 0f;
        }
    }

    [HarmonyPatch(typeof(ChatController), nameof(ChatController.AddChat))]
    class AddChatPatch
    {
        public static void Postfix(string chatText)
        {
            switch (chatText)
            {
                default:
                    break;
            }
            if (!AmongUsClient.Instance.AmHost) return;
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSendChat))]
    class RpcSendChatPatch
    {
        public static bool Prefix(PlayerControl __instance, string chatText, ref bool __result)
        {
            if (string.IsNullOrWhiteSpace(chatText))
            {
                __result = false;
                return false;
            }
            int return_count = PlayerControl.LocalPlayer.name.Count(x => x == '\n');
            chatText = new StringBuilder(chatText).Insert(0, "\n", return_count).ToString();
            if (AmongUsClient.Instance.AmClient && DestroyableSingleton<HudManager>.Instance)
                DestroyableSingleton<HudManager>.Instance.Chat.AddChat(__instance, chatText);
            if (chatText.IndexOf("who", StringComparison.OrdinalIgnoreCase) >= 0)
                DestroyableSingleton<Telemetry>.Instance.SendWho();
            MessageWriter messageWriter = AmongUsClient.Instance.StartRpc(__instance.NetId, (byte)RpcCalls.SendChat, SendOption.None);
            messageWriter.Write(chatText);
            messageWriter.EndMessage();
            __result = true;
            return false;
        }
    }
}