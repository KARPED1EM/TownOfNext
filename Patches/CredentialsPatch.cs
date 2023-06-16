using HarmonyLib;
using System.Text;
using TMPro;
using UnityEngine;

using static TOHE.Translator;

namespace TOHE;

[HarmonyPatch(typeof(PingTracker), nameof(PingTracker.Update))]
internal class PingTrackerUpdatePatch
{
    private static readonly StringBuilder sb = new();

    private static void Postfix(PingTracker __instance)
    {
        __instance.text.alignment = TextAlignmentOptions.TopRight;

        sb.Clear();

        sb.Append(Main.CredentialsText);

        var ping = AmongUsClient.Instance.Ping;
        string color = "#ff4500";
        if (ping < 50) color = "#44dfcc";
        else if (ping < 100) color = "#7bc690";
        else if (ping < 200) color = "#f3920e";
        else if (ping < 400) color = "#ff146e";
        sb.Append($"\r\n").Append($"<color={color}>Ping: {ping} ms</color>");

        if (Options.NoGameEnd.GetBool()) sb.Append($"\r\n").Append(Utils.ColorString(Color.red, GetString("NoGameEnd")));
        if (Options.AllowConsole.GetBool()) sb.Append($"\r\n").Append(Utils.ColorString(Color.red, GetString("AllowConsole")));
        if (!GameStates.IsModHost) sb.Append($"\r\n").Append(Utils.ColorString(Color.red, GetString("Warning.NoModHost")));
        if (DebugModeManager.IsDebugMode) sb.Append("\r\n").Append(Utils.ColorString(Color.green, GetString("DebugMode")));
        if (Options.LowLoadMode.GetBool()) sb.Append("\r\n").Append(Utils.ColorString(Color.green, GetString("LowLoadMode")));

        var offset_x = 1.2f; //右端からのオフセット
        if (HudManager.InstanceExists && HudManager._instance.Chat.ChatButton.active) offset_x += 0.8f; //チャットボタンがある場合の追加オフセット
        if (FriendsListManager.InstanceExists && FriendsListManager._instance.FriendsListButton.Button.active) offset_x += 0.8f; //フレンドリストボタンがある場合の追加オフセット
        __instance.GetComponent<AspectPosition>().DistanceFromEdge = new Vector3(offset_x, 0f, 0f);

        __instance.text.text = sb.ToString();
    }
}
[HarmonyPatch(typeof(VersionShower), nameof(VersionShower.Start))]
internal class VersionShowerStartPatch
{
    public static GameObject OVersionShower;
    private static TextMeshPro VisitText;
    private static void Postfix(VersionShower __instance)
    {
        Main.CredentialsText = $"\r\n<color={Main.ModColor}>{Main.ModName}</color> - {Main.PluginVersion}";
#if DEBUG
        Main.CredentialsText = $"\r\n<color=#00a4ff>{ThisAssembly.Git.Branch}</color> - {ThisAssembly.Git.Commit}";
#endif

        ErrorText.Create(__instance.text);
        if (Main.hasArgumentException && ErrorText.Instance != null)
            ErrorText.Instance.AddError(ErrorCode.Main_DictionaryError);

        if ((OVersionShower = GameObject.Find("VersionShower")) != null && VisitText == null)
        {
            VisitText = Object.Instantiate(__instance.text);
            VisitText.name = "TOHE User Counter";
            VisitText.alignment = TextAlignmentOptions.Left;
            VisitText.text = ModUpdater.visit > 0
                ? string.Format(GetString("TOHEVisitorCount"), Main.ModColor, ModUpdater.visit)
                : GetString("ConnectToTOHEServerFailed");
            VisitText.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
            VisitText.transform.localPosition = new Vector3(-3.92f, -2.9f, 0f);
            VisitText.enabled = GameObject.Find("TOHE Background") != null;

            __instance.text.alignment = TextAlignmentOptions.Left;
            OVersionShower.transform.localPosition = new Vector3(-4.92f, -3.3f, 0f);

            var ap1 = OVersionShower.GetComponent<AspectPosition>();
            if (ap1 != null) Object.Destroy(ap1);
            var ap2 = VisitText.GetComponent<AspectPosition>();
            if (ap2 != null) Object.Destroy(ap2);
        };
    }
}

[HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
[HarmonyPriority(Priority.First)]
internal class TitleLogoPatch
{
    public static GameObject ModStamp;
    public static GameObject Ambience;
    public static GameObject LeftPanel;
    public static GameObject RightPanel;
    public static GameObject Tint;
    public static GameObject Sizer;
    public static GameObject AULogo;
    public static GameObject BottomButtonBounds;

    public static Vector3 RightPanelOp;

    private static void Postfix(MainMenuManager __instance)
    {
        GameObject.Find("BackgroundTexture")?.SetActive(!MainMenuManagerPatch.ShowedBak);

        if ((ModStamp = GameObject.Find("ModStamp")) == null) return;
        ModStamp.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);

        if ((Ambience = GameObject.Find("Ambience")) == null) return;
        Ambience.SetActive(false);

        var TOHEBG = new GameObject("TOHE Background");
        TOHEBG.transform.position = new Vector3(0, 0, 520f);
        var bgRenderer = TOHEBG.AddComponent<SpriteRenderer>();
        bgRenderer.sprite = Utils.LoadSprite("TOHE.Resources.Images.TOHE-BG.jpg", 179f);

        if ((LeftPanel = GameObject.Find("LeftPanel")) == null) return;
        LeftPanel.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
        static void ResetParent(GameObject obj) => obj.transform.SetParent(LeftPanel.transform.parent);
        LeftPanel.ForEachChild((Il2CppSystem.Action<GameObject>)ResetParent);
        LeftPanel.SetActive(false);

        GameObject.Find("Divider")?.SetActive(false);

        if ((RightPanel = GameObject.Find("RightPanel")) == null) return;
        var rpap = RightPanel.GetComponent<AspectPosition>();
        if (rpap != null) Object.Destroy(rpap);
        RightPanelOp = RightPanel.transform.localPosition;
        RightPanel.transform.localPosition = RightPanelOp + new Vector3(10f, 0f, 0f);

        if ((Tint = GameObject.Find("Tint")) == null) return;
        var ttap = Tint.GetComponent<AspectPosition>();
        if (ttap != null) Object.Destroy(ttap);
        Tint.transform.SetParent(RightPanel.transform);
        Tint.transform.localPosition = new Vector3(-0.0824f, 0.0513f, Tint.transform.localPosition.z);
        Tint.transform.localScale = new Vector3(1f, 1f, 1f);

        if (!DebugModeManager.AmDebugger)
        {
            var scaler = RightPanel.transform.FindChild("MaskedBlackScreen").FindChild("GameModeButtons").FindChild("AspectSize").FindChild("Scaler");
            scaler.FindChild("FreePlayButton")?.gameObject.SetActive(false);
            scaler.FindChild("HowToPlayButton")?.gameObject.SetActive(false);
        }

        var creditsSizer = GameObject.Find("CreditsSizer");
        if (creditsSizer != null)
        {
            var creditsScreen = creditsSizer.transform.FindChild("CreditsScreen");
            if (creditsScreen != null)
            {
                var csto = creditsScreen.GetComponent<TransitionOpen>();
                if (csto != null) Object.Destroy(csto);
                var closeButton = creditsScreen.transform.FindChild("CloseButton");
                closeButton?.gameObject.SetActive(false);
            }
        }

        if ((Sizer = GameObject.Find("Sizer")) == null) return;
        if ((AULogo = GameObject.Find("LOGO-AU")) == null) return;
        Sizer.transform.localPosition += new Vector3(0f, 0.1f, 0f);
        AULogo.transform.position += new Vector3(0f, 0.1f, 0f);
        var logoRenderer = AULogo.GetComponent<SpriteRenderer>();
        logoRenderer.sprite = Utils.LoadSprite("TOHE.Resources.Images.TOHE-Logo.png");

        if ((BottomButtonBounds = GameObject.Find("BottomButtonBounds")) == null) return;
        BottomButtonBounds.transform.localPosition -= new Vector3(0f, 0.1f, 0f);
    }
}
[HarmonyPatch(typeof(ModManager), nameof(ModManager.LateUpdate))]
internal class ModManagerLateUpdatePatch
{
    public static void Prefix(ModManager __instance)
    {
        __instance.ShowModStamp();

        LateTask.Update(Time.deltaTime);
        CheckMurderPatch.Update();
    }
    public static void Postfix(ModManager __instance)
    {
        var offset_y = HudManager.InstanceExists ? 1.6f : 0.9f;
        __instance.ModStamp.transform.position = AspectPosition.ComputeWorldPosition(
            __instance.localCamera, AspectPosition.EdgeAlignments.RightTop,
            new Vector3(0.4f, offset_y, __instance.localCamera.nearClipPlane + 0.1f));
    }
}