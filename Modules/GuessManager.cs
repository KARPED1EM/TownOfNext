using HarmonyLib;
using Hazel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using TOHE.Roles.Neutral;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE;

public static class GuessManager
{

    public static string GetFormatString()
    {
        string text = GetString("PlayerIdList");
        foreach (var pc in Main.AllAlivePlayerControls)
        {
            string id = pc.PlayerId.ToString();
            string name = pc.GetRealName();
            text += $"\n{id} → {name}";
        }
        return text;
    }

    public static bool CheckCommond(ref string msg, string command, bool exact = true)
    {
        var comList = command.Split('|');
        for (int i = 0; i < comList.Count(); i++)
        {
            if (exact)
            {
                if (msg == "/" + comList[i]) return true;
            }
            else
            {
                if (msg.StartsWith("/" + comList[i]))
                {
                    msg = msg.Replace("/" + comList[i], string.Empty);
                    return true;
                }
            }
        }
        return false;
    }

    public static byte GetColorFromMsg(string msg)
    {
        if (ComfirmIncludeMsg(msg, "红|紅|red")) return 0;
        if (ComfirmIncludeMsg(msg, "蓝|藍|深蓝|blue")) return 1;
        if (ComfirmIncludeMsg(msg, "绿|綠|深绿|green")) return 2;
        if (ComfirmIncludeMsg(msg, "粉红|粉紅|pink")) return 3;
        if (ComfirmIncludeMsg(msg, "橘|橘|orange")) return 4;
        if (ComfirmIncludeMsg(msg, "黄|黃|yellow")) return 5;
        if (ComfirmIncludeMsg(msg, "黑|黑|black")) return 6;
        if (ComfirmIncludeMsg(msg, "白|白|white")) return 7;
        if (ComfirmIncludeMsg(msg, "紫|紫|perple")) return 8;
        if (ComfirmIncludeMsg(msg, "棕|棕|brown")) return 9;
        if (ComfirmIncludeMsg(msg, "青|青|cyan")) return 10;
        if (ComfirmIncludeMsg(msg, "黄绿|黃綠|浅绿|lime")) return 11;
        if (ComfirmIncludeMsg(msg, "红褐|紅褐|深红|maroon")) return 12;
        if (ComfirmIncludeMsg(msg, "玫红|玫紅|浅粉|rose")) return 13;
        if (ComfirmIncludeMsg(msg, "焦黄|焦黃|淡黄|banana")) return 14;
        if (ComfirmIncludeMsg(msg, "灰|灰|gray")) return 15;
        if (ComfirmIncludeMsg(msg, "茶|茶|tan")) return 16;
        if (ComfirmIncludeMsg(msg, "珊瑚|珊瑚|coral")) return 17;
        else return byte.MaxValue;
    }

    private static bool ComfirmIncludeMsg(string msg, string key)
    {
        var keys = key.Split('|');
        for (int i = 0; i < keys.Count(); i++)
        {
            if (msg.Contains(keys[i])) return true;
        }
        return false;
    }

    public static bool GuesserMsg(PlayerControl pc, string msg, bool isUI = false)
    {
        var originMsg = msg;

        if (!AmongUsClient.Instance.AmHost) return false;
        if (!GameStates.IsInGame || pc == null) return false;
        if (!pc.Is(CustomRoles.NiceGuesser) && !pc.Is(CustomRoles.EvilGuesser)) return false;

        int operate = 0; // 1:ID 2:猜测
        msg = msg.ToLower().TrimStart().TrimEnd();
        if (CheckCommond(ref msg, "id|guesslist|gl编号|玩家编号|玩家id|id列表|玩家列表|列表|所有id|全部id")) operate = 1;
        else if (CheckCommond(ref msg, "shoot|guess|bet|st|gs|bt|猜|赌", false)) operate = 2;
        else return false;

        if (!pc.IsAlive())
        {
            Utils.SendMessage(GetString("GuessDead"), pc.PlayerId);
            return true;
        }

        if (operate == 1)
        {
            Utils.SendMessage(GetFormatString(), pc.PlayerId);
            return true;
        }
        else if (operate == 2)
        {

            if (
            (pc.Is(CustomRoles.NiceGuesser) && Options.GGTryHideMsg.GetBool()) ||
            (pc.Is(CustomRoles.EvilGuesser) && Options.EGTryHideMsg.GetBool())
            ) TryHideMsg();
            else if (pc.AmOwner && !isUI) Utils.SendMessage(originMsg, 255, pc.GetRealName());

            if (!MsgToPlayerAndRole(msg, out byte targetId, out CustomRoles role, out string error))
            {
                Utils.SendMessage(error, pc.PlayerId);
                return true;
            }
            var target = Utils.GetPlayerById(targetId);
            if (target != null)
            {
                bool guesserSuicide = false;
                if (!Main.GuesserGuessed.ContainsKey(pc.PlayerId)) Main.GuesserGuessed.Add(pc.PlayerId, 0);
                if (pc.Is(CustomRoles.NiceGuesser) && Main.GuesserGuessed[pc.PlayerId] >= Options.GGCanGuessTime.GetInt())
                {
                    Utils.SendMessage(GetString("GGGuessMax"), pc.PlayerId);
                    return true;
                }
                if (pc.Is(CustomRoles.EvilGuesser) && Main.GuesserGuessed[pc.PlayerId] >= Options.EGCanGuessTime.GetInt())
                {
                    Utils.SendMessage(GetString("EGGuessMax"), pc.PlayerId);
                    return true;
                }
                if (role == CustomRoles.SuperStar && target.Is(CustomRoles.SuperStar))
                {
                    Utils.SendMessage(GetString("GuessSuperStar"), pc.PlayerId);
                    return true;
                }
                if (role == CustomRoles.GM || target.Is(CustomRoles.GM))
                {
                    Utils.SendMessage(GetString("GuessGM"), pc.PlayerId);
                    return true;
                }
                if (target.Is(CustomRoles.Snitch) && target.AllTasksCompleted())
                {
                    Utils.SendMessage(GetString("EGGuessSnitchTaskDone"), pc.PlayerId);
                    return true;
                }
                if (role.IsAdditionRole())
                {
                    if (
                        (pc.Is(CustomRoles.NiceGuesser) && !Options.GGCanGuessAdt.GetBool()) ||
                        (pc.Is(CustomRoles.EvilGuesser) && !Options.EGCanGuessAdt.GetBool())
                        )
                    {
                        Utils.SendMessage(GetString("GuessAdtRole"), pc.PlayerId);
                        return true;
                    }
                }
                if (pc.PlayerId == target.PlayerId)
                {
                    Utils.SendMessage(GetString("LaughToWhoGuessSelf"), pc.PlayerId, Utils.ColorString(Color.cyan, GetString("MessageFromKPD")));
                    guesserSuicide = true;
                }
                else if (pc.Is(CustomRoles.NiceGuesser) && role.IsCrewmate() && !Options.GGCanGuessCrew.GetBool() && !pc.Is(CustomRoles.Madmate)) guesserSuicide = true;
                else if (pc.Is(CustomRoles.EvilGuesser) && role.IsImpostor() && !Options.EGCanGuessImp.GetBool()) guesserSuicide = true;
                else if (!target.Is(role)) guesserSuicide = true;

                var dp = guesserSuicide ? pc : target;
                target = dp;

                string Name = dp.GetRealName();

                Main.GuesserGuessed[pc.PlayerId]++;

                new LateTask(() =>
                {
                    Main.PlayerStates[dp.PlayerId].deathReason = PlayerState.DeathReason.Gambled;
                    dp.SetRealKiller(pc);
                    RpcGuesserMurderPlayer(dp);

                    //死者检查
                    FixedUpdatePatch.LoversSuicide(target.PlayerId);
                    if (target.Is(CustomRoles.Terrorist))
                    {
                        Logger.Info(target?.Data?.PlayerName + "はTerroristだった", "MurderPlayer");
                        Utils.CheckTerroristWin(target.Data);
                    }
                    if (Executioner.Target.ContainsValue(target.PlayerId))
                        Executioner.ChangeRoleByTarget(target);

                    Utils.NotifyRoles(isForMeeting: true, NoCache: true);

                    new LateTask(() => { Utils.SendMessage(string.Format(GetString("GuessKill"), Name), 255, Utils.ColorString(Utils.GetRoleColor(CustomRoles.NiceGuesser), GetString("GuessKillTitle"))); }, 0.6f, "Guess Msg");

                }, 0.2f, "Guesser Kill");
            }
        }
        return true;
    }

    public static TMPro.TextMeshPro nameText(this PlayerControl p) => p.cosmetics.nameText;
    public static TMPro.TextMeshPro NameText(this PoolablePlayer p) => p.cosmetics.nameText;
    public static void RpcGuesserMurderPlayer(this PlayerControl pc, float delay = 0f) //ゲッサー用の殺し方
    {
        // DEATH STUFF //
        var amOwner = pc.AmOwner;
        pc.Data.IsDead = true;
        pc.RpcExileV2();
        Main.PlayerStates[pc.PlayerId].SetDead();
        var meetingHud = MeetingHud.Instance;
        var hudManager = DestroyableSingleton<HudManager>.Instance;
        SoundManager.Instance.PlaySound(pc.KillSfx, false, 0.8f);
        hudManager.KillOverlay.ShowKillAnimation(pc.Data, pc.Data);
        if (amOwner)
        {
            hudManager.ShadowQuad.gameObject.SetActive(false);
            pc.nameText().GetComponent<MeshRenderer>().material.SetInt("_Mask", 0);
            pc.RpcSetScanner(false);
            ImportantTextTask importantTextTask = new GameObject("_Player").AddComponent<ImportantTextTask>();
            importantTextTask.transform.SetParent(AmongUsClient.Instance.transform, false);
            meetingHud.SetForegroundForDead();
        }
        PlayerVoteArea voteArea = MeetingHud.Instance.playerStates.First(
            x => x.TargetPlayerId == pc.PlayerId
        );
        if (voteArea == null) return;
        if (voteArea.DidVote) voteArea.UnsetVote();
        voteArea.AmDead = true;
        voteArea.Overlay.gameObject.SetActive(true);
        voteArea.Overlay.color = Color.white;
        voteArea.XMark.gameObject.SetActive(true);
        voteArea.XMark.transform.localScale = Vector3.one;
        foreach (var playerVoteArea in meetingHud.playerStates)
        {
            if (playerVoteArea.VotedFor != pc.PlayerId) continue;
            playerVoteArea.UnsetVote();
            var voteAreaPlayer = Utils.GetPlayerById(playerVoteArea.TargetPlayerId);
            if (!voteAreaPlayer.AmOwner) continue;
            meetingHud.ClearVote();
        }
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.GuessKill, SendOption.Reliable, -1);
        writer.Write(pc.PlayerId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void RpcClientGuess(PlayerControl pc)
    {
        var amOwner = pc.AmOwner;
        var meetingHud = MeetingHud.Instance;
        var hudManager = DestroyableSingleton<HudManager>.Instance;
        SoundManager.Instance.PlaySound(pc.KillSfx, false, 0.8f);
        hudManager.KillOverlay.ShowKillAnimation(pc.Data, pc.Data);
        if (amOwner)
        {
            hudManager.ShadowQuad.gameObject.SetActive(false);
            pc.nameText().GetComponent<MeshRenderer>().material.SetInt("_Mask", 0);
            pc.RpcSetScanner(false);
            ImportantTextTask importantTextTask = new GameObject("_Player").AddComponent<ImportantTextTask>();
            importantTextTask.transform.SetParent(AmongUsClient.Instance.transform, false);
            meetingHud.SetForegroundForDead();
        }
        PlayerVoteArea voteArea = MeetingHud.Instance.playerStates.First(
            x => x.TargetPlayerId == pc.PlayerId
        );
        //pc.Die(DeathReason.Kill);
        if (voteArea == null) return;
        if (voteArea.DidVote) voteArea.UnsetVote();
        voteArea.AmDead = true;
        voteArea.Overlay.gameObject.SetActive(true);
        voteArea.Overlay.color = Color.white;
        voteArea.XMark.gameObject.SetActive(true);
        voteArea.XMark.transform.localScale = Vector3.one;
        foreach (var playerVoteArea in meetingHud.playerStates)
        {
            if (playerVoteArea.VotedFor != pc.PlayerId) continue;
            playerVoteArea.UnsetVote();
            var voteAreaPlayer = Utils.GetPlayerById(playerVoteArea.TargetPlayerId);
            if (!voteAreaPlayer.AmOwner) continue;
            meetingHud.ClearVote();
        }
    }
    private static bool MsgToPlayerAndRole(string msg, out byte id, out CustomRoles role, out string error)
    {
        if (msg.StartsWith("/")) msg = msg.Replace("/", string.Empty);

        Regex r = new("\\d+");
        MatchCollection mc = r.Matches(msg);
        string result = string.Empty;
        for (int i = 0; i < mc.Count; i++)
        {
            result += mc[i];//匹配结果是完整的数字，此处可以不做拼接的
        }

        if (int.TryParse(result, out int num))
        {
            id = Convert.ToByte(num);
        }
        else
        {
            //并不是玩家编号，判断是否颜色
            //byte color = GetColorFromMsg(msg);
            //好吧我不知道怎么取某位玩家的颜色，等会了的时候再来把这里补上
            id = byte.MaxValue;
            error = GetString("GuessHelp");
            role = new();
            return false;
        }

        //判断选择的玩家是否合理
        PlayerControl target = Utils.GetPlayerById(id);
        if (target == null || target.Data.IsDead)
        {
            error = GetString("GuessNull");
            role = new();
            return false;
        }

        if (!ChatCommands.GetRoleByName(msg, out role))
        {
            error = GetString("GuessHelp");
            return false;
        }

        error = string.Empty;
        return true;
    }

    public static void TryHideMsg()
    {
        ChatUpdatePatch.DoBlockChat = true;
        List<CustomRoles> roles = Enum.GetValues(typeof(CustomRoles)).Cast<CustomRoles>().Where(x => x is not CustomRoles.NotAssigned and not CustomRoles.KB_Normal).ToList();
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
                CustomRoles role = roles[rd.Next(0, roles.Count())];
                msg += rd.Next(1, 100) < 50 ? string.Empty : " ";
                msg += Utils.GetRoleName(role);
            }
            var player = Main.AllAlivePlayerControls.ToArray()[rd.Next(0, Main.AllAlivePlayerControls.Count())];
            DestroyableSingleton<HudManager>.Instance.Chat.AddChat(player, msg);
            var writer = CustomRpcSender.Create("MessagesToSend", SendOption.None);
            writer.StartMessage(-1);
            writer.StartRpc(player.NetId, (byte)RpcCalls.SendChat)
                .Write(msg)
                .EndRpc();
            writer.EndMessage();
            writer.SendMessage();
        }
        ChatUpdatePatch.DoBlockChat = false;
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    class StartMeetingPatch
    {
        public static void Postfix(MeetingHud __instance)
        {
            if (PlayerControl.LocalPlayer.GetCustomRole() is CustomRoles.NiceGuesser or CustomRoles.EvilGuesser && PlayerControl.LocalPlayer.IsAlive())
                CreateGuesserButton(__instance);
        }
    }
    public static void CreateGuesserButton(MeetingHud __instance)
    {
        for (int i = 0; i < __instance.playerStates.Length; i++)
        {
            PlayerVoteArea playerVoteArea = __instance.playerStates[i];
            if (Main.PlayerStates[playerVoteArea.TargetPlayerId].IsDead) continue;

            GameObject template = playerVoteArea.Buttons.transform.Find("CancelButton").gameObject;
            GameObject targetBox = UnityEngine.Object.Instantiate(template, playerVoteArea.transform);
            targetBox.name = "ShootButton";
            targetBox.transform.localPosition = new Vector3(-0.95f, 0.03f, -1.3f);
            SpriteRenderer renderer = targetBox.GetComponent<SpriteRenderer>();
            renderer.sprite = Utils.LoadSprite("TOHE.Resources.Images.Skills.TargetIcon.png", 115f);
            PassiveButton button = targetBox.GetComponent<PassiveButton>();
            button.OnClick.RemoveAllListeners();
            int copiedIndex = i;
            button.OnClick.AddListener((Action)(() => GuesserOnClick(copiedIndex, __instance)));
        }
    }

    public const int MaxOneScreenRole = 40;
    public static int Page;
    public static PassiveButton ExitButton;
    public static GameObject guesserUI;
    private static Dictionary<CustomRoleTypes, List<Transform>> RoleButtons;
    private static Dictionary<CustomRoleTypes, SpriteRenderer> RoleSelectButtons;
    private static List<SpriteRenderer> PageButtons;
    public static CustomRoleTypes currentTeamType;
    static void GuesserSelectRole(CustomRoleTypes Role, bool SetPage = true)
    {
        currentTeamType = Role;
        if (SetPage) Page = 1;
        foreach (var RoleButton in RoleButtons)
        {
            int index = 0;
            foreach (var RoleBtn in RoleButton.Value)
            {
                if (RoleBtn == null) continue;
                index++;
                if (index <= (Page - 1) * 40) { RoleBtn.gameObject.SetActive(false); continue; }
                if ((Page * 40) < index) { RoleBtn.gameObject.SetActive(false); continue; }
                RoleBtn.gameObject.SetActive(RoleButton.Key == Role);
            }
        }
        foreach (var RoleButton in RoleSelectButtons)
        {
            if (RoleButton.Value == null) continue;
            RoleButton.Value.color = new(0, 0, 0, RoleButton.Key == Role ? 1 : 0.25f);
        }
    }

    public static TextMeshPro textTemplate;
    static void GuesserOnClick(int buttonTarget, MeetingHud __instance)
    {
        var pc = Utils.GetPlayerById(buttonTarget);
        if (pc == null || !pc.IsAlive()) return;

        if (guesserUI != null || !(__instance.state == MeetingHud.VoteStates.Voted || __instance.state == MeetingHud.VoteStates.NotVoted)) return;
        if (__instance.playerStates[buttonTarget].AmDead) return;
        Page = 1;
        RoleButtons = new();
        RoleSelectButtons = new();
        PageButtons = new();
        __instance.playerStates.ToList().ForEach(x => x.gameObject.SetActive(false));

        Transform container = UnityEngine.Object.Instantiate(GameObject.Find("PhoneUI").transform, __instance.transform);
        container.transform.localPosition = new Vector3(0, 0, -200f);
        guesserUI = container.gameObject;

        List<int> i = new() { 0, 0, 0 };
        var buttonTemplate = __instance.playerStates[0].transform.FindChild("votePlayerBase");
        var maskTemplate = __instance.playerStates[0].transform.FindChild("MaskArea");
        var smallButtonTemplate = __instance.playerStates[0].Buttons.transform.Find("CancelButton");
        textTemplate.enabled = true;
        if (textTemplate.transform.FindChild("RoleTextMeeting") != null) UnityEngine.Object.Destroy(textTemplate.transform.FindChild("RoleTextMeeting").gameObject);

        Transform exitButtonParent = new GameObject().transform;
        exitButtonParent.SetParent(container);
        Transform exitButton = UnityEngine.Object.Instantiate(buttonTemplate, exitButtonParent);
        exitButton.FindChild("ControllerHighlight").gameObject.SetActive(false);
        Transform exitButtonMask = UnityEngine.Object.Instantiate(maskTemplate, exitButtonParent);
        exitButton.gameObject.GetComponent<SpriteRenderer>().sprite = smallButtonTemplate.GetComponent<SpriteRenderer>().sprite;
        exitButtonParent.transform.localPosition = new Vector3(2.725f, 2.1f, -200f);
        exitButtonParent.transform.localScale = new Vector3(0.25f, 0.9f, 1f);
        exitButtonParent.transform.SetAsFirstSibling();
        exitButton.GetComponent<PassiveButton>().OnClick.RemoveAllListeners();
        exitButton.GetComponent<PassiveButton>().OnClick.AddListener((System.Action)(() =>
        {
            __instance.playerStates.ToList().ForEach(x => x.gameObject.SetActive(true));
            UnityEngine.Object.Destroy(container.gameObject);
        }));
        ExitButton = exitButton.GetComponent<PassiveButton>();

        List<Transform> buttons = new();
        Transform selectedButton = null;

        for (int index = 0; index < 3; index++)
        {
            Transform TeambuttonParent = new GameObject().transform;
            TeambuttonParent.SetParent(container);
            Transform Teambutton = UnityEngine.Object.Instantiate(buttonTemplate, TeambuttonParent);
            Teambutton.FindChild("ControllerHighlight").gameObject.SetActive(false);
            Transform TeambuttonMask = UnityEngine.Object.Instantiate(maskTemplate, TeambuttonParent);
            TextMeshPro Teamlabel = UnityEngine.Object.Instantiate(textTemplate, Teambutton);
            Teambutton.GetComponent<SpriteRenderer>().sprite = DestroyableSingleton<HatManager>.Instance.GetNamePlateById("nameplate_NoPlate")?.viewData?.viewData?.Image;
            RoleSelectButtons.Add((CustomRoleTypes)index, Teambutton.GetComponent<SpriteRenderer>());
            TeambuttonParent.localPosition = new(-2.75f + (index * 1.75f), 2.225f, -200);
            TeambuttonParent.localScale = new(0.55f, 0.55f, 1f);
            Teamlabel.color = (CustomRoleTypes)index is CustomRoleTypes.Crewmate ? new Color32(140, 255, 255, byte.MaxValue) : ((CustomRoleTypes)index is CustomRoleTypes.Impostor ? new Color32(255, 25, 25, byte.MaxValue) : new Color32(255, 171, 27, byte.MaxValue));
            Logger.Info(Teamlabel.color.ToString(), ((CustomRoleTypes)index).ToString());
            Teamlabel.text = GetString(((CustomRoleTypes)index is CustomRoleTypes.Crewmate ? "Crewmate" : "Team" + ((CustomRoleTypes)index).ToString()));
            Teamlabel.alignment = TMPro.TextAlignmentOptions.Center;
            Teamlabel.transform.localPosition = new Vector3(0, 0, Teamlabel.transform.localPosition.z);
            Teamlabel.transform.localScale *= 1.6f;
            Teamlabel.autoSizeTextContainer = true;

            static void CreateTeamButton(Transform Teambutton, CustomRoleTypes type)
            {
                Teambutton.GetComponent<PassiveButton>().OnClick.AddListener((UnityEngine.Events.UnityAction)(() =>
                {
                    GuesserSelectRole(type);
                    ReloadPage();
                }));
            }
            if (PlayerControl.LocalPlayer.IsAlive()) CreateTeamButton(Teambutton, (CustomRoleTypes)index);
        }
        static void ReloadPage()
        {
            PageButtons[0].color = new(1, 1, 1, 1f);
            PageButtons[1].color = new(1, 1, 1, 1f);
            if ((RoleButtons[currentTeamType].Count / MaxOneScreenRole + (RoleButtons[currentTeamType].Count % MaxOneScreenRole != 0 ? 1 : 0)) < Page)
            {
                Page -= 1;
                PageButtons[1].color = new(1, 1, 1, 0.1f);
            }
            else if ((RoleButtons[currentTeamType].Count / MaxOneScreenRole + (RoleButtons[currentTeamType].Count % MaxOneScreenRole != 0 ? 1 : 0)) < Page + 1)
            {
                PageButtons[1].color = new(1, 1, 1, 0.1f);
            }
            if (Page <= 1)
            {
                Page = 1;
                PageButtons[0].color = new(1, 1, 1, 0.1f);
            }
            GuesserSelectRole(currentTeamType, false);
            Logger.Info("Page:" + Page, "Guesser UI");
        }
        static void CreatePage(bool IsNext, MeetingHud __instance, Transform container)
        {
            var buttonTemplate = __instance.playerStates[0].transform.FindChild("votePlayerBase");
            var maskTemplate = __instance.playerStates[0].transform.FindChild("MaskArea");
            var smallButtonTemplate = __instance.playerStates[0].Buttons.transform.Find("CancelButton");
            Transform PagebuttonParent = new GameObject().transform;
            PagebuttonParent.SetParent(container);
            Transform Pagebutton = UnityEngine.Object.Instantiate(buttonTemplate, PagebuttonParent);
            Pagebutton.FindChild("ControllerHighlight").gameObject.SetActive(false);
            Transform PagebuttonMask = UnityEngine.Object.Instantiate(maskTemplate, PagebuttonParent);
            TextMeshPro Pagelabel = UnityEngine.Object.Instantiate(textTemplate, Pagebutton);
            Pagebutton.GetComponent<SpriteRenderer>().sprite = DestroyableSingleton<HatManager>.Instance.GetNamePlateById("nameplate_NoPlate")?.viewData?.viewData?.Image;
            PagebuttonParent.localPosition = IsNext ? new(3.535f, -2.2f, -200) : new(-3.475f, -2.2f, -200);
            PagebuttonParent.localScale = new(0.55f, 0.55f, 1f);
            Pagelabel.color = Color.white;
            Pagelabel.text = GetString(IsNext ? "NextPage" : "PreviousPage");
            Pagelabel.alignment = TMPro.TextAlignmentOptions.Center;
            Pagelabel.transform.localPosition = new Vector3(0, 0, Pagelabel.transform.localPosition.z);
            Pagelabel.transform.localScale *= 1.6f;
            Pagelabel.autoSizeTextContainer = true;
            if (!IsNext && Page <= 1) Pagebutton.GetComponent<SpriteRenderer>().color = new(1, 1, 1, 0.1f);
            Pagebutton.GetComponent<PassiveButton>().OnClick.AddListener((Action)(() => ClickEvent()));
            void ClickEvent()
            {
                Logger.Info("Click", "Guesser UI");
                if (IsNext) Page += 1;
                else Page -= 1;
                ReloadPage();
            }
            PageButtons.Add(Pagebutton.GetComponent<SpriteRenderer>());
        }
        if (PlayerControl.LocalPlayer.IsAlive())
        {
            CreatePage(false, __instance, container);
            CreatePage(true, __instance, container);
        }
        int ind = 0;
        foreach (CustomRoles role in Enum.GetValues(typeof(CustomRoles)))
        {
            if (role.IsAdditionRole() ||
                role.IsVanilla() ||
                role is CustomRoles.GM or CustomRoles.NotAssigned or CustomRoles.KB_Normal
                ) continue;
            CreateRole(role);
        }
        void CreateRole(CustomRoles role)
        {
            if (40 <= i[(int)role.GetCustomRoleTypes()]) i[(int)role.GetCustomRoleTypes()] = 0;
            Transform buttonParent = new GameObject().transform;
            buttonParent.SetParent(container);
            Transform button = UnityEngine.Object.Instantiate(buttonTemplate, buttonParent);
            button.FindChild("ControllerHighlight").gameObject.SetActive(false);
            Transform buttonMask = UnityEngine.Object.Instantiate(maskTemplate, buttonParent);
            TextMeshPro label = UnityEngine.Object.Instantiate(textTemplate, button);

            button.GetComponent<SpriteRenderer>().sprite = DestroyableSingleton<HatManager>.Instance.GetNamePlateById("nameplate_NoPlate")?.viewData?.viewData?.Image;
            if (!RoleButtons.ContainsKey(role.GetCustomRoleTypes()))
            {
                RoleButtons.Add(role.GetCustomRoleTypes(), new());
            }
            RoleButtons[role.GetCustomRoleTypes()].Add(button);
            buttons.Add(button);
            int row = i[(int)role.GetCustomRoleTypes()] / 5;
            int col = i[(int)role.GetCustomRoleTypes()] % 5;
            buttonParent.localPosition = new Vector3(-3.47f + 1.75f * col, 1.5f - 0.45f * row, -200f);
            buttonParent.localScale = new Vector3(0.55f, 0.55f, 1f);
            label.text = GetString(role.ToString());
            label.color = Utils.GetRoleColor(role);
            label.alignment = TMPro.TextAlignmentOptions.Center;
            label.transform.localPosition = new Vector3(0, 0, label.transform.localPosition.z);
            label.transform.localScale *= 1.6f;
            label.autoSizeTextContainer = true;
            int copiedIndex = i[(int)role.GetCustomRoleTypes()];

            button.GetComponent<PassiveButton>().OnClick.RemoveAllListeners();
            if (PlayerControl.LocalPlayer.IsAlive()) button.GetComponent<PassiveButton>().OnClick.AddListener((System.Action)(() =>
            {
                if (selectedButton != button)
                {
                    selectedButton = button;
                    buttons.ForEach(x => x.GetComponent<SpriteRenderer>().color = x == selectedButton ? Utils.GetRoleColor(PlayerControl.LocalPlayer.GetCustomRole()) : Color.white);
                }
                else
                {
                    var focusedTarget = Utils.GetPlayerById(__instance.playerStates[buttonTarget].TargetPlayerId);
                    if (!(__instance.state == MeetingHud.VoteStates.Voted || __instance.state == MeetingHud.VoteStates.NotVoted) || focusedTarget == null || !PlayerControl.LocalPlayer.IsAlive()) return;

                    Logger.Msg($"Click: {__instance.playerStates[buttonTarget].TargetPlayerId}({focusedTarget.GetCustomRole()}) => {role}", "Guesser UI");

                    if (AmongUsClient.Instance.AmHost) GuesserMsg(PlayerControl.LocalPlayer, $"/bt {__instance.playerStates[buttonTarget].TargetPlayerId} {GetString(role.ToString())}", true);
                    else SendRPC(__instance.playerStates[buttonTarget].TargetPlayerId, role);

                    // Reset the GUI
                    __instance.playerStates.ToList().ForEach(x => x.gameObject.SetActive(true));
                    UnityEngine.Object.Destroy(container.gameObject);
                    textTemplate.enabled = false;

                }
            }));
            i[(int)role.GetCustomRoleTypes()]++;
            ind++;
        }
        container.transform.localScale *= 0.75f;
        GuesserSelectRole(CustomRoleTypes.Crewmate);
        ReloadPage();
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.OnDestroy))]
    class MeetingHudOnDestroyGuesserUIClose
    {
        public static void Postfix()
        {
            UnityEngine.Object.Destroy(textTemplate.gameObject);
        }
    }

    private static void SendRPC(int playerId, CustomRoles role)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.Guess, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write((byte)role);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader, PlayerControl pc)
    {
        int PlayerId = reader.ReadInt32();
        CustomRoles role = (CustomRoles)reader.ReadByte();
        GuesserMsg(pc, $"/bt {PlayerId} {GetString(role.ToString())}");
    }
}
