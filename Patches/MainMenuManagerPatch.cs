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
    public static GameObject Template;
    public static GameObject InviteButton;
    public static GameObject WebsiteButton;
    public static GameObject updateButton;

    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.OpenGameModeMenu)), HarmonyPrefix]
    public static void OpenGameModeMenu_Prefix(MainMenuManager __instance)
    {
        if (!TitleLogoPatch.RightPanel.active) TitleLogoPatch.RightPanel.SetActive(true);
    }
    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.OpenAccountMenu)), HarmonyPrefix]
    public static void OpenAccountMenu_Prefix(MainMenuManager __instance)
    {
        if (!TitleLogoPatch.RightPanel.active) TitleLogoPatch.RightPanel.SetActive(true);
    }
    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.OpenCredits)), HarmonyPrefix]
    public static void OpenCredits_Prefix(MainMenuManager __instance)
    {
        if (!TitleLogoPatch.RightPanel.active) TitleLogoPatch.RightPanel.SetActive(true);
    }

    static bool isOnline = false;
    public static bool showed = false;
    [HarmonyPatch(typeof(SignInStatusComponent), nameof(SignInStatusComponent.SetOnline)), HarmonyPostfix]
    public static void SetOnline_Postfix(SignInStatusComponent __instance) => new LateTask(() => { isOnline = true; }, 1.5f, "Set Online Status");
    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.LateUpdate)), HarmonyPostfix]
    public static void MainMenuManager_LateUpdate(SignInStatusComponent __instance)
    {
        if (showed || !isOnline) return;
        var bak = GameObject.Find("BackgroundTexture");
        if (bak == null || !bak.active) return;
        var pos = bak.transform.position;
        Vector3 lerp = Vector3.Lerp(pos, new Vector3(pos.x, 7.1f, pos.z), Time.deltaTime * 1.4f);
        bak.transform.position = lerp;
        if (pos.y > 7f) showed = true;
    }

    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start)), HarmonyPrefix]
    public static void Start_Prefix(MainMenuManager __instance)
    {
        if (Template == null) Template = GameObject.Find("ExitGameButton");
        if (Template == null) return;
        int row = 1; int col = 0;
        GameObject CreatButton(string text, Action action)
        { 
            col++; if (col > 2) { col = 1; row++; }
            var button = Object.Instantiate(Template, Template.transform.parent);
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

        Application.targetFrameRate = Main.UnlockFPS.Value ? 165 : 60;

        return;

        //Updateボタンを生成
        if (updateButton == null) updateButton = Object.Instantiate(Template, Template.transform.parent);
        updateButton.name = "UpdateButton";
        updateButton.transform.position = Template.transform.position + new Vector3(0.25f, 0.75f);
        updateButton.transform.GetChild(0).GetComponent<RectTransform>().localScale *= 1.5f;

        var updateText = updateButton.transform.GetChild(0).GetComponent<TMPro.TMP_Text>();
        UnityEngine.Color updateColor = new Color32(247, 56, 23, byte.MaxValue);
        PassiveButton updatePassiveButton = updateButton.GetComponent<PassiveButton>();
        SpriteRenderer updateButtonSprite = updateButton.GetComponent<SpriteRenderer>();
        updatePassiveButton.OnClick = new();
        updatePassiveButton.OnClick.AddListener((Action)(() =>
        {
            updateButton.SetActive(false);
            ModUpdater.StartUpdate(ModUpdater.downloadUrl);
        }));
        updateText.DestroyTranslator();
        updatePassiveButton.OnMouseOut.AddListener((Action)(() => updateButtonSprite.color = updateText.color = updateColor));
        updateButtonSprite.color = updateText.color = updateColor;
        updateButtonSprite.size *= 1.5f;
        updateButton.SetActive(false);
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
