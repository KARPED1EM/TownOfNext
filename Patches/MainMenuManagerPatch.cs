using AmongUs.Data;
using AmongUs.Data.Player;
using Assets.InnerNet;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TOHE;

[HarmonyPatch]
public class MainMenuManagerPatch
{
    public static GameObject Template_Left;
    public static GameObject Template_Right;
    public static GameObject Template_Main;
    public static GameObject InviteButton;
    public static GameObject WebsiteButton;
    public static GameObject UpdateButton;

    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.OpenGameModeMenu)), HarmonyPrefix]
    public static void OpenGameModeMenu_Prefix(MainMenuManager __instance) => ShowingPanel = true;
    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.OpenAccountMenu)), HarmonyPrefix]
    public static void OpenAccountMenu_Prefix(MainMenuManager __instance) => ShowingPanel = true;
    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.OpenCredits)), HarmonyPrefix]
    public static void OpenCredits_Prefix(MainMenuManager __instance) => ShowingPanel = true;
    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start)), HarmonyPrefix]
    public static void Start_Postfix(MainMenuManager __instance) => ShowingPanel = false;
    [HarmonyPatch(typeof(OptionsMenuBehaviour), nameof(OptionsMenuBehaviour.Open)), HarmonyPrefix]
    public static void OpenOptionsMenu_Postfix(MainMenuManager __instance) => ShowingPanel = false;

    private static bool isOnline = false;
    public static bool ShowedBak = false;
    private static bool ShowingPanel = false;
    [HarmonyPatch(typeof(SignInStatusComponent), nameof(SignInStatusComponent.SetOnline)), HarmonyPostfix]
    public static void SetOnline_Postfix(SignInStatusComponent __instance) => new LateTask(() => { isOnline = true; }, 0.2f, "Set Online Status");
    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.LateUpdate)), HarmonyPostfix]
    public static void MainMenuManager_LateUpdate(SignInStatusComponent __instance)
    {
        if (GameObject.Find("MainUI") == null) ShowingPanel = false;

        var pos1 = TitleLogoPatch.RightPanel.transform.localPosition;
        Vector3 lerp1 = Vector3.Lerp(pos1, TitleLogoPatch.RightPanelOp + new Vector3((ShowingPanel ? 0f : 10f), 0f, 0f), Time.deltaTime * (ShowingPanel ? 3f : 2f));
        TitleLogoPatch.RightPanel.transform.localPosition = lerp1;

        if (ShowedBak || !isOnline) return;
        var bak = GameObject.Find("BackgroundTexture");
        if (bak == null || !bak.active) return;
        var pos2 = bak.transform.position;
        Vector3 lerp2 = Vector3.Lerp(pos2, new Vector3(pos2.x, 7.1f, pos2.z), Time.deltaTime * 1.4f);
        bak.transform.position = lerp2;
        if (pos2.y > 7f) ShowedBak = true;
    }

    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start)), HarmonyPrefix]
    public static void Start_Prefix(MainMenuManager __instance)
    {
        if (Template_Left == null) Template_Left = GameObject.Find("CreditsButton");
        if (Template_Right == null) Template_Right = GameObject.Find("ExitGameButton");
        if (Template_Left == null || Template_Right == null) return;
        int row = 1; int col = 0;
        GameObject CreatButton(string text, Action action)
        { 
            col++; if (col > 2) { col = 1; row++; }
            var template = col == 1 ? Template_Left : Template_Right;
            var button = Object.Instantiate(template, template.transform.parent);
            button.transform.transform.FindChild("FontPlacer").GetChild(0).gameObject.DestroyTranslator();
            var buttonText = button.transform.FindChild("FontPlacer").GetChild(0).GetComponent<TextMeshPro>();
            buttonText.text = text;
            PassiveButton passiveButton = button.GetComponent<PassiveButton>();
            passiveButton.OnClick = new();
            passiveButton.OnClick.AddListener(action);
            AspectPosition aspectPosition = button.GetComponent<AspectPosition>();
            aspectPosition.anchorPoint = new Vector2(col == 1 ? 0.415f : 0.583f, 0.5f - 0.08f * row);
            return button;
        }

        bool china = CultureInfo.CurrentCulture.Name == "zh-CN";
        if (InviteButton == null) InviteButton = CreatButton(china ? "QQ群" : "Discord", () => { Application.OpenURL(china ? Main.QQInviteUrl : Main.DiscordInviteUrl); });
        InviteButton.gameObject.SetActive(china ? Main.ShowQQButton : Main.ShowDiscordButton);
        InviteButton.name = "TOHE Invite Button";

        if (WebsiteButton == null) WebsiteButton = CreatButton(Translator.GetString("Website"), () => Application.OpenURL("https://tohe.cc"));
        WebsiteButton.gameObject.SetActive(Main.ShowWebsiteButton);
        WebsiteButton.name = "TOHE Website Button";

        if (UpdateButton == null)
        {
            Template_Main = GameObject.Find("PlayButton");
            UpdateButton = Object.Instantiate(Template_Main, Template_Main.transform.parent);
            UpdateButton.name = "TOHE Update Button";
            UpdateButton.transform.localPosition = Template_Main.transform.localPosition - new Vector3(0f, 0f, 3f);
            var inactive = UpdateButton.transform.FindChild("Inactive");
            var spriteRenderer = inactive.GetComponent<SpriteRenderer>();
            spriteRenderer.color = new Color32(255, 120, 255, byte.MaxValue);
            var shine = inactive.FindChild("Shine");
            var shineSpriteRenderer = shine.GetComponent<SpriteRenderer>();
            shineSpriteRenderer.color = new Color32(50, 200, 255, byte.MaxValue);
            var passiveButton = UpdateButton.GetComponent<PassiveButton>();
            passiveButton.OnClick = new();
            passiveButton.OnClick.AddListener((Action)(() =>
            {
                Template_Main.SetActive(true);
                UpdateButton.SetActive(false);
                if (!DebugModeManager.AmDebugger || !Input.GetKey(KeyCode.LeftShift))
                    ModUpdater.StartUpdate(ModUpdater.downloadUrl);
            }));
            UpdateButton.transform.transform.FindChild("FontPlacer").GetChild(0).gameObject.DestroyTranslator();
            Template_Main.SetActive(false);
        }

        Application.targetFrameRate = Main.UnlockFPS.Value ? 165 : 60;
    }
}

// 来源：https://github.com/ykundesu/SuperNewRoles/blob/master/SuperNewRoles/Patches/HorseModePatch.cs
[HarmonyPatch(typeof(Constants), nameof(Constants.ShouldHorseAround))]
public static class HorseModePatch
{
    public static bool isHorseMode = false;
    public static bool Prefix(ref bool __result)
    {
        __result = isHorseMode;
        return false;
    }
}

// 参考：https://github.com/Yumenopai/TownOfHost_Y
public class ModNews
{
    public int Number;
    public uint Lang;
    public int BeforeNumber;
    public string Title;
    public string SubTitle;
    public string ShortTitle;
    public string Text;
    public string Date;

    public Announcement ToAnnouncement()
    {
        var result = new Announcement
        {
            Number = Number,
            Language = Lang,
            Title = Title,
            SubTitle = SubTitle,
            ShortTitle = ShortTitle,
            Text = Text,
            Date = Date,
            Id = "ModNews"
        };

        return result;
    }
}

[HarmonyPatch]
public class ModNewsHistory
{
    public static List<ModNews> AllModNews = new();

    [HarmonyPatch(typeof(AnnouncementPopUp), nameof(AnnouncementPopUp.Init)), HarmonyPostfix]
    public static void Initialize(ref Il2CppSystem.Collections.IEnumerator __result)
    {
        static IEnumerator GetEnumerator()
        {
            while (AnnouncementPopUp.UpdateState == AnnouncementPopUp.AnnounceState.Fetching) yield return null;
            if (AnnouncementPopUp.UpdateState > AnnouncementPopUp.AnnounceState.Fetching && DataManager.Player.Announcements.AllAnnouncements.Count > 0) yield break;

            AnnouncementPopUp.UpdateState = AnnouncementPopUp.AnnounceState.Fetching;
            AllModNews.Clear();

            var lang = DataManager.Settings.Language.CurrentLanguage.ToString();
            if (!Assembly.GetExecutingAssembly().GetManifestResourceNames().Any(x => x.StartsWith($"TOHE.Resources.ModNews.{lang}.")))
                lang = SupportedLangs.English.ToString();

            var fileNames = Assembly.GetExecutingAssembly().GetManifestResourceNames().Where(x => x.StartsWith($"TOHE.Resources.ModNews.{lang}."));
            foreach (var file in fileNames)
                AllModNews.Add(GetContentFromRes(file));

            AnnouncementPopUp.UpdateState = AnnouncementPopUp.AnnounceState.NotStarted;
        }

        __result = Effects.Sequence(GetEnumerator().WrapToIl2Cpp(), __result);
    }

    public static ModNews GetContentFromRes(string path)
    {
        ModNews mn = new();
        var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path);
        stream.Position = 0;
        using StreamReader reader = new(stream, Encoding.UTF8);
        string text = "";
        uint langId = (uint)DataManager.Settings.Language.CurrentLanguage;
        //uint langId = (uint)SupportedLangs.SChinese;
        while (!reader.EndOfStream)
        {
            string line = reader.ReadLine();
            if (line.StartsWith("#Number:")) mn.Number = int.Parse(line.Replace("#Number:", string.Empty));
            else if (line.StartsWith("#LangId:")) langId = uint.Parse(line.Replace("#LangId:", string.Empty));
            else if (line.StartsWith("#Title:")) mn.Title = line.Replace("#Title:", string.Empty);
            else if (line.StartsWith("#SubTitle:")) mn.SubTitle = line.Replace("#SubTitle:", string.Empty);
            else if (line.StartsWith("#ShortTitle:")) mn.ShortTitle = line.Replace("#ShortTitle:", string.Empty);
            else if (line.StartsWith("#Date:")) mn.Date = line.Replace("#Date:", string.Empty);
            else if (line.StartsWith("#---")) continue;
            else
            {
                if (line.StartsWith("## ")) line = line.Replace("## ", "<b>") + "</b>";
                else if (line.StartsWith("- ")) line = line.Replace("- ", "・");
                text += $"\n{line}";
            }
        }
        mn.Lang = langId;
        mn.Text = text;
        Logger.Info($"Number:{mn.Number}", "ModNews");
        Logger.Info($"Title:{mn.Title}", "ModNews");
        Logger.Info($"SubTitle:{mn.SubTitle}", "ModNews");
        Logger.Info($"ShortTitle:{mn.ShortTitle}", "ModNews");
        Logger.Info($"Date:{mn.Date}", "ModNews");
        return mn;
    }

    [HarmonyPatch(typeof(PlayerAnnouncementData), nameof(PlayerAnnouncementData.SetAnnouncements)), HarmonyPrefix]
    public static bool SetModAnnouncements(PlayerAnnouncementData __instance, [HarmonyArgument(0)] Il2CppReferenceArray<Announcement> aRange)
    {
        List<Announcement> list = new();
        foreach (var a in aRange) list.Add(a);
        foreach (var m in AllModNews) list.Add(m.ToAnnouncement());
        list.Sort((a1, a2) => { return DateTime.Compare(DateTime.Parse(a2.Date), DateTime.Parse(a1.Date)); });

        __instance.allAnnouncements = new Il2CppSystem.Collections.Generic.List<Announcement>();
        foreach (var a in list) __instance.allAnnouncements.Add(a);


        __instance.HandleChange();
        __instance.OnAddAnnouncement?.Invoke();

        return false;
    }
}
