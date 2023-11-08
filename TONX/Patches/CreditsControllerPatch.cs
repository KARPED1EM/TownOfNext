using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using static TONX.Translator;

namespace TONX;

[HarmonyPatch(typeof(CreditsController))]
public class CreditsControllerPatch
{
    private static bool ModCreditsAdded = false;
    private static List<CreditsController.CreditStruct> ModCredits = GetModCredits();

    private static List<CreditsController.CreditStruct> GetModCredits()
    {
        var devList = new List<string>()
            {
                //$"<color=#bd262a><size=150%>{GetString("FromChina")}</size></color>",

                $"KARPED1EM - {GetString("Creater")}",
                $"SHAAARKY - {GetString("Collaborators")}",
                $"IRIDESCENT - {GetString("Art")}",
                $"Endrmen40409 - {GetString("Art")}",
                $"天寸梦初 - {GetString("PullRequester")}",
                $"NCSIMON - {GetString("PullRequester")}",
                $"喜 - {GetString("PullRequester")}",
                $"Tommy-XL - {GetString("PullRequester")}",
                $"Commandf1 - {GetString("Contributor")}",
                $"水木年华 - {GetString("Contributor")}",
                $"SolarFlare - {GetString("Contributor")}",
                $"Mousse - {GetString("Contributor")}",
            };
        var translatorList = new List<string>()
            {
                $"Tommy-XL - {GetString("TranEN")}&{GetString("TranRU")}",
                $"Tem - {GetString("TranEN")}&{GetString("TranRU")}",
                $"阿龍 - {GetString("TranCHT")}",
                $"Gurge44 - {GetString("TranEN")}",
                $"法官 - {GetString("TranCHT")}",
                $"SolarFlare - {GetString("TranEN")}",
                $"chill_ultimated - {GetString("TranRU")}"
            };
        var acList = new List<string>()
            {
                //Mods
                $"{GetString("TownOfHost")}",
                $"{GetString("TownOfHost_Y")}",
                $"{GetString("TownOfHost-TheOtherRoles")}",
                $"{GetString("SuperNewRoles")}",
                $"{GetString("Project-Lotus")}",

                // Sponsor
                $"罗寄",
                $"鬼",
                $"喜",
                $"小叨院长",
                $"波奇酱",
                $"法师",
                $"沐煊",
                $"SolarFlare",
                $"林林林",
                $"撒币",
                $"斯卡蒂Skadi",
                $"ltemten",
                $"Night_瓜",
                $"群诱饵",
                $"Slok",
                $"辣鸡",
                $"湛蓝色",
                $"小黄117",
                $"chun",
                $"Z某",
                $"Shark",
                $"清风awa",
                $"1 1 1 1",

                //Discord Server Booster
                $"bunny",
                $"Loonie",
                $"Namra",
                $"KNIGHT",
                $"SolarFlare",
                $"Bluéfôx.",
                $"shiftyrose",
                $"M ™",
                $"yunfi",
            };

        var credits = new List<CreditsController.CreditStruct>();

        AddTitleToCredits(Utils.ColorString(Main.ModColor32, Main.ModName));
        AddPersonToCredits(devList);
        AddSpcaeToCredits();

        AddTitleToCredits(GetString("Translator"));
        AddPersonToCredits(translatorList);
        AddSpcaeToCredits();

        AddTitleToCredits(GetString("Acknowledgement"));
        AddPersonToCredits(acList);
        AddSpcaeToCredits();

        return credits;

        void AddSpcaeToCredits()
        {
            AddTitleToCredits(string.Empty);
        }
        void AddTitleToCredits(string title)
        {
            credits.Add(new()
            {
                format = "title",
                columns = new[] { title },
            });
        }
        void AddPersonToCredits(List<string> list)
        {
            foreach(var line in list)
            {
                var cols = line.Split(" - ").ToList();
                if (cols.Count < 2) cols.Add(string.Empty);
                credits.Add(new()
                {
                    format = "person",
                    columns = cols.ToArray(),
                });
            }
        }
    }

    [HarmonyPatch(nameof(CreditsController.AddCredit)), HarmonyPrefix]
    public static void AddCreditPrefix(CreditsController __instance, [HarmonyArgument(0)] CreditsController.CreditStruct originalCredit)
    {
        if (ModCreditsAdded) return;
        ModCreditsAdded = true;

        foreach(var credit in ModCredits)
        {
            __instance.AddCredit(credit);
            __instance.AddFormat(credit.format);
        }
    }
}