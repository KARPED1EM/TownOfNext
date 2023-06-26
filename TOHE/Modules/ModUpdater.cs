using HarmonyLib;
using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE;

[HarmonyPatch]
public class ModUpdater
{
    public static bool IsInChina => CultureInfo.CurrentCulture.Name == "zh-CN";
    private static readonly string[] URLs =
    {
#if DEBUG
        "file:///D:/Desktop/TOHE/Release/info.json",
#else
        "https://raw.githubusercontent.com/KARPED1EM/TOHE-Dev/TOHE/Release/info.json",
        "https://gitee.com/leeverz/TOHE-Dev/raw/TOHE/Release/info.json",
        "https://cdn.jsdelivr.net/gh/KARPED1EM/TOHE-Dev/Release/info.json",
        "https://tohe-next-1301425958.cos.ap-shanghai.myqcloud.com/info.json",
#endif
    };

    public static bool hasUpdate = false;
    public static bool forceUpdate = false;
    public static bool isBroken = false;
    public static bool isChecked = false;

    public static string versionInfoRaw = "";

    public static Version latestVersion = null;
    public static Version minimumVersion = null;
    public static int creation = 0;
    public static string md5 = "";
    public static int visit => isChecked ? 216822 : 0; //只能手动更新了

    public static string announcement_zh = "";
    public static string announcement_en = "";
    public static string downloadUrl_github = "";
    public static string downloadUrl_gitee = "";
    public static string downloadUrl_cos = "";

    public static GameObject Fill;
    public static GameObject InfoScreen;
    public static PassiveButton ActionButton;

    public static Task updateTask;

    private static int retried = 0;

    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start)), HarmonyPostfix, HarmonyPriority(Priority.LowerThanNormal)]
    public static void StartPostfix()
    {
        var DOBScreen = AccountManager.Instance.transform.FindChild("DOBEnterScreen");
        if (DOBScreen != null && (Fill == null || InfoScreen == null || ActionButton == null))
        {
            Fill = UnityEngine.Object.Instantiate(DOBScreen.FindChild("Fill").gameObject);
            Fill.transform.SetLocalZ(-100f);
            Fill.name = "TOHE Info Popup Fill";
            Fill.SetActive(false);
            InfoScreen = UnityEngine.Object.Instantiate(DOBScreen.FindChild("InfoPage").gameObject);
            InfoScreen.transform.SetLocalZ(-110f);
            InfoScreen.name = "TOHE Info Popup Page";
            InfoScreen.SetActive(false);
            ActionButton = InfoScreen.transform.FindChild("BackButton").GetComponent<PassiveButton>();
            ActionButton.gameObject.name = "ActionButton";
        }

        if (!isChecked) CheckForUpdate();

        SetUpdateButtonStatus();
    }
    public static void SetUpdateButtonStatus()
    {
        MainMenuManagerPatch.UpdateButton.SetActive(isChecked && hasUpdate);
        MainMenuManagerPatch.PlayButton.SetActive(!MainMenuManagerPatch.UpdateButton.activeSelf);
        var buttonText = MainMenuManagerPatch.UpdateButton.transform.FindChild("FontPlacer").GetChild(0).GetComponent<TextMeshPro>();
        buttonText.text = $"{GetString("updateButton")}\nv{latestVersion?.ToString() ?? "???"}";
    }
    public static void Retry()
    {
        retried++;
        ShowPopup(GetString("updateCheckPopupTitle"), GetString("PleaseWait"));
        new LateTask(CheckForUpdate, 0.3f, "Retry Check Update");
    }
    public static void CheckForUpdate()
    {
        isChecked = false;
        DeleteOldFiles();

        foreach (var url in URLs)
        {
            if (GetVersionInfo(url).GetAwaiter().GetResult())
            {
                isChecked = true;
                break;
            }
        }

        Logger.Msg("Check For Update: " + isChecked, "CheckRelease");
        isBroken = !isChecked;
        if (isChecked)
        {
            Logger.Info("Has Update: " + hasUpdate, "CheckRelease");
            Logger.Info("Latest Version: " + latestVersion.ToString(), "CheckRelease");
            Logger.Info("Minimum Version: " + minimumVersion.ToString(), "CheckRelease");
            Logger.Info("Creation: " + creation.ToString(), "CheckRelease");
            Logger.Info("Force Update: " + forceUpdate, "CheckRelease");
            Logger.Info("File MD5: " + md5, "CheckRelease");
            Logger.Info("Github Url: " + downloadUrl_github, "CheckRelease");
            Logger.Info("Gitee Url: " + downloadUrl_github, "CheckRelease");
            Logger.Info("COS Url: " + downloadUrl_gitee, "CheckRelease");
            Logger.Info("Announcement (English): " + announcement_en, "CheckRelease");
            Logger.Info("Announcement (SChinese): " + announcement_zh, "CheckRelease");

            if ((!Main.AlreadyShowMsgBox || isBroken))
            {
                Main.AlreadyShowMsgBox = true;
                var annos = IsInChina ? announcement_zh : announcement_en;
                if (isBroken) ShowPopup(GetString(StringNames.AnnouncementLabel), annos, GetString(StringNames.ExitGame), Application.Quit);
                else ShowPopup(GetString(StringNames.AnnouncementLabel), annos, GetString(StringNames.Okay));
            }
        }
        else
        {
            if (retried >= 2) ShowPopup(GetString("updateCheckPopupTitle"), GetString("updateCheckFailedExit"), GetString(StringNames.Okay));
            else ShowPopup(GetString("updateCheckPopupTitle"), GetString("updateCheckFailedRetry"), GetString("Retry"), Retry);
        }

        SetUpdateButtonStatus();
    }
    public static string Get(string url)
    {
        string result = "";
        HttpClient req = new HttpClient();
        var res = req.GetAsync(url).Result;
        Stream stream = res.Content.ReadAsStreamAsync().Result;
        try
        {
            //获取内容
            using StreamReader reader = new(stream);
            result = reader.ReadToEnd();
        }
        finally
        {
            stream.Close();
        }
        return result;
    }
    public static async Task<bool> GetVersionInfo(string url)
    {
        Logger.Msg(url, "CheckRelease");
        try
        {
            string result;
            if (url.StartsWith("file:///"))
            {
                result = File.ReadAllText(url[8..]);
            }
            else
            {
                using HttpClient client = new();
                client.DefaultRequestHeaders.Add("User-Agent", "TOHE Updater");
                client.DefaultRequestHeaders.Add("Referer", "tohe.cc");
                using var response = await client.GetAsync(new Uri(url), HttpCompletionOption.ResponseContentRead);
                if (!response.IsSuccessStatusCode || response.Content == null)
                {
                    Logger.Error($"Failed: {response.StatusCode}", "CheckRelease");
                    return false;
                }
                result = await response.Content.ReadAsStringAsync();
                result = result.Replace("\r", string.Empty).Replace("\n", string.Empty).Trim();
            }

            JObject data = JObject.Parse(result);

            latestVersion = new(data["version"]?.ToString());
            var minVer = data["minVer"]?.ToString();
            minimumVersion = minVer.ToLower() == "latest" ? latestVersion : new(minVer);
            creation = int.Parse(data["creation"]?.ToString());
            isBroken = data["allowStart"]?.ToString().ToLower() != "true";
            md5 = data["md5"]?.ToString();

            JObject announcement = data["announcement"].Cast<JObject>();
            announcement_en = announcement["English"]?.ToString();
            announcement_zh = announcement["SChinese"]?.ToString();

            JObject downloadUrl = data["url"].Cast<JObject>();
            downloadUrl_github = downloadUrl["github"]?.ToString().Replace("{{version}}", $"v{latestVersion}");
            downloadUrl_gitee = downloadUrl["gitee"]?.ToString();
            downloadUrl_cos = downloadUrl["cos"]?.ToString();

            hasUpdate = Main.version < latestVersion;
            forceUpdate = Main.version < minimumVersion || creation > Main.PluginCreation;

            return true;
        }
        catch (Exception ex)
        {
            Logger.Error($"Exception:\n{ex.Message}", "CheckRelease", false);
            return false;
        }
    }
    public static void StartUpdate(string url = "")
    {
        if (url == "") url = IsInChina ? downloadUrl_gitee : downloadUrl_github;
        ShowPopup(GetString("updatePopupTitle"), GetString("updatePleaseWait"));
        updateTask = DownloadDLL(url);
    }
    public static void DeleteOldFiles()
    {
        try
        {
            foreach (var path in Directory.EnumerateFiles(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "*.*"))
            {
                if (path.EndsWith(Path.GetFileName(Assembly.GetExecutingAssembly().Location))) continue;
                if (path.EndsWith("TOHE.dll")) continue;
                Logger.Info($"{Path.GetFileName(path)} 已删除", "DeleteOldFiles");
                File.Delete(path);
            }
        }
        catch (Exception e)
        {
            Logger.Error($"清除更新残留失败\n{e}", "DeleteOldFiles");
        }
        return;
    }
    public static async Task<bool> DownloadDLL(string url)
    {
        var savePath = "BepInEx/plugins/TOHE.dll.temp";
        File.Delete(savePath);

        HttpClient client = new HttpClient();
        HttpResponseMessage response = await client.GetAsync(url);
        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            switch (ex.StatusCode)
            {
                case HttpStatusCode.NotFound:
                    ShowPopup(GetString("updatePopupTitleFialed"), GetString("HttpNotFound"), GetString(StringNames.Okay));
                    break;
                case HttpStatusCode.Forbidden:
                    ShowPopup(GetString("updatePopupTitleFialed"), GetString("HttpForbidden"), GetString(StringNames.Okay));
                    break;
                default:
                    ShowPopup(GetString("updatePopupTitleFialed"), ex.Message, GetString(StringNames.Okay));
                    break;
            }
            return false;
        }

        try
        {
            var fileSize = response.Content.Headers.ContentLength.Value;
            var stream = await response.Content.ReadAsStreamAsync();
            using (var fileStream = File.Create(savePath))
            using (stream)
            {
                byte[] buffer = new byte[1024];
                var readLength = 0;
                int length;
                long lastUpdateTime = 0;
                while ((length = await stream.ReadAsync(buffer)) != 0)
                {
                    readLength += length;
                    int progress = (int)((double)readLength / fileSize * 100);
                    if (lastUpdateTime != Utils.GetTimeStamp())
                        ShowPopup(GetString("updatePopupTitle"), $"<size=150%>{GetString("updateInProgress")}\n{readLength}/{fileSize}\n({progress}%)");
                    lastUpdateTime = Utils.GetTimeStamp();
                    fileStream.Write(buffer, 0, length);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"更新失败\n{ex}", "DownloadDLL", false);
            ShowPopup(GetString("updatePopupTitleFialed"), GetString("downloadFailed"), GetString(StringNames.ExitGame), Application.Quit);
            return false;
        }

        if (GetMD5HashFromFile(savePath) != md5)
        {
            File.Delete(savePath);
            ShowPopup(GetString("updatePopupTitleFialed"), GetString("updateFileMd5Incorrect"), GetString(StringNames.Okay));
            MainMenuManagerPatch.UpdateButton.SetActive(true);
            MainMenuManagerPatch.PlayButton.SetActive(false);
        }
        else
        {
            var fileName = Assembly.GetExecutingAssembly().Location;
            File.Move(fileName, fileName + ".bak");
            File.Move("BepInEx/plugins/TOHE.dll.temp", fileName);
            ShowPopup(GetString("updatePopupTitleDone"), GetString("updateRestart"), GetString(StringNames.ExitGame), Application.Quit);
        }
        return true;
    }
    public static string GetMD5HashFromFile(string fileName)
    {
        try
        {
            FileStream file = new(fileName, FileMode.Open);
            MD5 md5 = MD5.Create();
            byte[] retVal = md5.ComputeHash(file);
            file.Close();

            StringBuilder sb = new();
            for (int i = 0; i < retVal.Length; i++) sb.Append(retVal[i].ToString("x2"));
            return sb.ToString();
        }
        catch (Exception ex)
        {
            Logger.Exception(ex, "GetMD5HashFromFile");
            return "";
        }
    }
    private static void ShowPopup(string title, string message, string buttonText = null, Action buttonAction = null)
    {
        if (Fill == null || InfoScreen == null) return;
        var titleTmp = InfoScreen.transform.FindChild("Title Text").GetComponent<TextMeshPro>();
        titleTmp.transform.localPosition = new(0f, 2.3f, 3f);
        titleTmp.DestroyTranslator();
        titleTmp.text = title;
        InfoScreen.transform.FindChild("InfoText_TMP").GetComponent<RectTransform>().sizeDelta = new(7f, 1.3f);
        var infoTmp = InfoScreen.transform.FindChild("InfoText_TMP").GetComponent<TextMeshPro>();
        infoTmp.transform.localScale = new(1f, 1f, 1f);
        infoTmp.DestroyTranslator();
        infoTmp.text = message;
        Fill.SetActive(true);
        if (ActionButton == null || buttonText == null) 
        {
            ActionButton?.gameObject?.SetActive(buttonText != null);
            InfoScreen.SetActive(true);
            return;
        }
        ActionButton.transform.localScale = new(0.66f, 0.66f, 0.66f);
        ActionButton.transform.localPosition = new(0f, -0.65f, 3f);
        var buttonTmp = ActionButton.transform.FindChild("Text_TMP").GetComponent<TextMeshPro>();
        buttonTmp.DestroyTranslator();
        buttonTmp.text = buttonText ?? GetString(StringNames.Okay);
        ActionButton.OnClick = new();
        ActionButton.OnClick.AddListener((Action)(() =>
        {
            Fill.SetActive(false);
            InfoScreen.SetActive(false);
            buttonAction?.Invoke();
        }));
        ActionButton.gameObject.SetActive(true);
        InfoScreen.SetActive(true);
    }
}