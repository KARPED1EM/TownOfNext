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
    private static readonly string URL_2018k = "http://api.2018k.cn";
    private static readonly string URL_Github = "https://api.github.com/repos/KARPED1EM/TownOfHostEdited";
    public static bool hasUpdate = false;
    public static bool forceUpdate = true;
    public static bool isBroken = false;
    public static bool isChecked = false;
    public static Version latestVersion = null;
    public static string latestTitle = null;
    public static string downloadUrl = null;
    public static string md5 = null;
    public static string notice = null;
    public static int visit = 0;
    public static GenericPopup InfoPopup;
    public static GameObject PopupButton;
    public static Task updateTask;

    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start)), HarmonyPrefix]
    [HarmonyPriority(2)]
    public static void Start_Prefix(MainMenuManager __instance)
    {
        NewVersionCheck();
        DeleteOldFiles();
        InfoPopup = UnityEngine.Object.Instantiate(Twitch.TwitchManager.Instance.TwitchPopup);
        InfoPopup.name = "TOHE Info Popup";
        InfoPopup.TextAreaTMP.GetComponent<RectTransform>().sizeDelta = new(2.5f, 2f);
        PopupButton = InfoPopup.transform.FindChild("ExitGame").gameObject;
        PopupButton.name = "Action Button";
        PopupButton.transform.localPosition -= new Vector3(0f, 0.3f, 0f);
        PopupButton.transform.localScale *= 0.85f;
        InfoPopup.gameObject.transform.FindChild("Background").transform.localScale *= 1.4f;
        if (!isChecked)
        {
            bool done;
            if (CultureInfo.CurrentCulture.Name == "zh-CN")
            {
                done = CheckRelease().GetAwaiter().GetResult();
            }
            else
            {
                done = CheckReleaseFromGithub(Main.BetaBuildURL.Value != "").GetAwaiter().GetResult();
                done = CheckRelease(done).GetAwaiter().GetResult();
            }
            Logger.Msg("检查更新结果: " + done, "CheckRelease");
            Logger.Info("hasupdate: " + hasUpdate, "CheckRelease");
            Logger.Info("forceupdate: " + forceUpdate, "CheckRelease");
            Logger.Info("downloadUrl: " + downloadUrl, "CheckRelease");
            Logger.Info("latestVersionl: " + latestVersion, "CheckRelease");
        }
        MainMenuManagerPatch.PlayButton.SetActive(!hasUpdate);
        MainMenuManagerPatch.UpdateButton.SetActive(hasUpdate);
        var buttonText = MainMenuManagerPatch.UpdateButton.transform.FindChild("FontPlacer").GetChild(0).GetComponent<TextMeshPro>();
        buttonText.text = $"{GetString("updateButton")}\n{latestTitle}";
    }

    public static string UrlSetId(string url) => url + "?id=36EA7F566EB74B5D8E584E653685EDF5";
    public static string UrlSetCheck(string url) => url + "/checkVersion";
    public static string UrlSetInfo(string url) => url + "/getExample";
    public static string UrlSetToday(string url) => url + "/today";

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

    public static Task<bool> CheckRelease(bool onlyInfo = false)
    {
        Logger.Msg("开始从2018k检查更新", "CheckRelease");
        string url = UrlSetId(UrlSetCheck(URL_2018k)) + "&version=" + Main.PluginVersion;
        try
        {
            string res = Get(url);
            string[] info = res.Split("|");
            if (!onlyInfo)
            {
                hasUpdate = false;
                forceUpdate = info[1] == "true";
                latestVersion = new(info[4]);
                latestTitle = "Ver. " + info[4];

                string[] num = info[4].Split(".");
                string[] inum = Main.PluginVersion.Split(".");
                if (num.Length > inum.Length) inum.AddItem("0");
                for (int i = 0; i < num.Length; i++)
                {
                    int c = int.Parse(num[i]);
                    int m = int.Parse(inum[i]);
                    if (c > m) hasUpdate = true;
                    if (c != m) break;
                }
            }
            if (downloadUrl == null || downloadUrl == "") downloadUrl = info[3];

            url = UrlSetId(UrlSetInfo(URL_2018k)) + "&data=remark|notice|md5|visit";
            string[] data = Get(url).Split("|");
            string[] notices = data[1].Split("\n&&\n");
            if (CultureInfo.CurrentCulture.Name.StartsWith("zh")) notice = notices[0];
            else notice = notices[1];
            md5 = data[2];
            visit = int.TryParse(data[3], out int x) ? x : 0;
            visit += 216822; //旧版本数据
            var create = 1;
            if (int.TryParse(data[0], out int ct) && ct < 1000) create = ct;
            if (create > Main.PluginCreate)
            {
                hasUpdate = true;
                forceUpdate = true;
            }

            if (!Main.AlreadyShowMsgBox || create == 0)
            {
                Main.AlreadyShowMsgBox = true;
                if (create == 0) ShowPopup(notice, GetString(StringNames.ExitGame), Application.Quit);
                else ShowPopup(notice, GetString(StringNames.Okay));
            }

            Logger.Info("hasupdate: " + info[0], "2018k");
            Logger.Info("forceupdate: " + info[1], "2018k");
            Logger.Info("downloadUrl: " + info[3], "2018k");
            Logger.Info("latestVersionl: " + info[4], "2018k");
            Logger.Info("remark: " + data[0], "2018k");
            Logger.Info("notice: " + notice, "2018k");
            Logger.Info("MD5: " + data[2], "2018k");
            Logger.Info("Visit: " + data[3], "2018k");

            if (downloadUrl == null || downloadUrl == "")
            {
                Logger.Error("获取下载地址失败", "CheckRelease");
                return Task.FromResult(false);
            }

            isChecked = true;
            isBroken = false;
        }
        catch (Exception ex)
        {
            if (CultureInfo.CurrentCulture.Name == "zh-CN")
            {
                isChecked = false;
                isBroken = true;
            }
            else if (!onlyInfo)
            {
                isChecked = true;
                isBroken = false;
                Logger.Error($"检查更新时发生错误\n{ex}", "CheckRelease", false);
            }
            Logger.Error($"检查更新时发生错误，已忽略\n{ex}", "CheckRelease", false);
            return Task.FromResult(false);
        }
        return Task.FromResult(true);
    }
    public static async Task<bool> CheckReleaseFromGithub(bool beta = false)
    {
        Logger.Msg("开始从Github检查更新", "CheckRelease");
        string url = beta ? Main.BetaBuildURL.Value : URL_Github + "/releases/latest";
        try
        {
            string result;
            using (HttpClient client = new())
            {
                client.DefaultRequestHeaders.Add("User-Agent", "TOHE Updater");
                using var response = await client.GetAsync(new Uri(url), HttpCompletionOption.ResponseContentRead);
                if (!response.IsSuccessStatusCode || response.Content == null)
                {
                    Logger.Error($"状态码: {response.StatusCode}", "CheckRelease");
                    return false;
                }
                result = await response.Content.ReadAsStringAsync();
            }
            JObject data = JObject.Parse(result);
            if (beta)
            {
                latestTitle = data["name"].ToString();
                downloadUrl = data["url"].ToString();
                hasUpdate = latestTitle != ThisAssembly.Git.Commit;
            }
            else
            {
                latestVersion = new(data["tag_name"]?.ToString().TrimStart('v'));
                latestTitle = $"Ver. {latestVersion}";
                JArray assets = data["assets"].Cast<JArray>();
                for (int i = 0; i < assets.Count; i++)
                {
                    if (assets[i]["name"].ToString() == "TOHE_Steam.dll" && Constants.GetPlatformType() == Platforms.StandaloneSteamPC)
                    {
                        downloadUrl = assets[i]["browser_download_url"].ToString();
                        break;
                    }
                    if (assets[i]["name"].ToString() == "TOHE_Epic.dll" && Constants.GetPlatformType() == Platforms.StandaloneEpicPC)
                    {
                        downloadUrl = assets[i]["browser_download_url"].ToString();
                        break;
                    }
                    if (assets[i]["name"].ToString() == "TOHE.dll")
                        downloadUrl = assets[i]["browser_download_url"].ToString();
                }
                hasUpdate = latestVersion.CompareTo(Main.version) > 0;
            }

            Logger.Info("hasupdate: " + hasUpdate, "Github");
            Logger.Info("forceupdate: " + forceUpdate, "Github");
            Logger.Info("downloadUrl: " + downloadUrl, "Github");
            Logger.Info("latestVersionl: " + latestVersion, "Github");
            Logger.Info("latestTitle: " + latestTitle, "Github");

            if (downloadUrl == null || downloadUrl == "")
            {
                Logger.Error("获取下载地址失败", "CheckRelease");
                return false;
            }
            isChecked = true;
            isBroken = false;
        }
        catch (Exception ex)
        {
            isBroken = true;
            Logger.Error($"发布检查失败\n{ex}", "CheckRelease", false);
            return false;
        }
        return true;
    }
    public static void StartUpdate(string url)
    {
        ShowPopup(GetString("updatePleaseWait"));
        updateTask = DownloadDLL(url);
    }
    public static bool NewVersionCheck()
    {
        try
        {
            var fileName = Assembly.GetExecutingAssembly().Location;
            if (Directory.Exists("TOH_DATA") && File.Exists(@"./TOHE_DATA/BanWords.txt"))
            {
                DirectoryInfo di = new("TOH_DATA");
                di.Delete(true);
                Logger.Warn("删除旧数据：TOH_DATA", "NewVersionCheck");
            }
        }
        catch (Exception ex)
        {
            Logger.Exception(ex, "NewVersionCheck");
            return false;
        }
        return true;
    }
    public static bool BackOldDLL()
    {
        try
        {
            foreach (var path in Directory.EnumerateFiles(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "*.dll"))
            {
                Logger.Info($"{Path.GetFileName(path)} 已删除", "BackOldDLL");
                File.Delete(path);
            }
            File.Move(Assembly.GetExecutingAssembly().Location + ".bak", Assembly.GetExecutingAssembly().Location);
        }
        catch
        {
            Logger.Error("回退老版本失败", "BackOldDLL");
            return false;
        }
        return true;
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
    private static readonly object downloadLock = new();
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
                    ShowPopup(GetString("HttpNotFound"));
                    break;
                case HttpStatusCode.Forbidden:
                    ShowPopup(GetString("HttpForbidden"));
                    break;
                default:
                    ShowPopup(ex.Message);
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
                        ShowPopup($"<size=150%>{GetString("updateInProgress")}\n{readLength}/{fileSize}\n({progress}%)");
                    lastUpdateTime = Utils.GetTimeStamp();
                    fileStream.Write(buffer, 0, length);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"更新失败\n{ex}", "DownloadDLL", false);
            ShowPopup(GetString("updateManually"), GetString(StringNames.ExitGame), Application.Quit);
            return false;
        }

        if (GetMD5HashFromFile(savePath) != md5)
        {
            File.Delete(savePath);
            ShowPopup(GetString("downloadFailed"), GetString(StringNames.Okay));
            MainMenuManagerPatch.UpdateButton.SetActive(true);
            MainMenuManagerPatch.PlayButton.SetActive(false);
        }
        else
        {
            var fileName = Assembly.GetExecutingAssembly().Location;
            File.Move(fileName, fileName + ".bak");
            File.Move("BepInEx/plugins/TOHE.dll.temp", fileName);
            ShowPopup(GetString("updateRestart"), GetString(StringNames.ExitGame), Application.Quit);
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
    private static void ShowPopup(string message, string buttonText = null, Action buttonAction = null)
    {
        if (InfoPopup == null) return;
        InfoPopup.Show(message);
        if (PopupButton == null) return;
        PopupButton.gameObject.SetActive(buttonText != null);
        PopupButton.transform.GetChild(0).gameObject.DestroyTranslator();
        var tmp = PopupButton.transform.GetChild(0).GetComponent<TextMeshPro>();
        tmp.SetText(buttonText);
        PopupButton.GetComponent<PassiveButton>().OnClick = new();
        PopupButton.GetComponent<PassiveButton>().OnClick.AddListener((Action)(() => 
        {
            InfoPopup.Close();
            buttonAction?.Invoke(); 
        }));
    }
}