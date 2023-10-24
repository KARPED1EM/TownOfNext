using HarmonyLib;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;

namespace TONX;

# pragma warning disable CA1416
public static class RegistryManager
{
    public static RegistryKey SoftwareKeys => Registry.CurrentUser.OpenSubKey("Software", true);
    public static RegistryKey Keys = SoftwareKeys.OpenSubKey("AU-TONX", true);
    public static Version LastVersion;

    public static void Init()
    {
        if (Keys == null)
        {
            Logger.Info("Create TONX Registry Key", "Registry Manager");
            Keys = SoftwareKeys.CreateSubKey("AU-TONX", true);
        }
        if (Keys == null)
        {
            Logger.Error("Create Registry Failed", "Registry Manager");
            return;
        }

        if (Keys.GetValue("Last launched version") is not string regLastVersion)
            LastVersion = new Version(0, 0, 0);
        else LastVersion = Version.Parse(regLastVersion);

        Keys.SetValue("Last launched version", Main.version.ToString());
        Keys.SetValue("Path", Path.GetFullPath("./"));

        List<string> FoldersToDel = new()
            {
                @"./TOH_DATA",
                @"./TOHE_DATA",
                @"./TONX_DATA",
            };

        Logger.Warn("上次启动的TONX版本：" + LastVersion, "Registry Manager");

        if (LastVersion < new Version(3, 0, 0))
        {
            Logger.Warn("v3.0.0 New Version Operation Needed", "Registry Manager");
            FoldersToDel.Add(@"./BepInEx/config");
        }

        FoldersToDel.DoIf(Directory.Exists, p =>
        {
            Logger.Warn("Delete Useless Directory:" + p, "Registry Manager");
            Directory.Delete(p, true);
        });
    }
}
