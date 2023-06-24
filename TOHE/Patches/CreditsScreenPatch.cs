using HarmonyLib;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE;

[HarmonyPatch(typeof(CreditsScreenPopUp))]
public class CreditsScreenPatch
{
    private static GameObject TOHELogo;
    [HarmonyPatch(nameof(CreditsScreenPopUp.OnEnable)), HarmonyPrefix]
    public static void Prefix(CreditsScreenPopUp __instance)
    {
        if (TOHELogo == null)
        {
            var devsList = new List<string>()
            {
                $"<color=#bd262a><size=150%>{GetString("FromChina")}</size></color>",

                "\t",

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
            var translatorsList = new List<string>()
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

                "\t",

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

                "\t",

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

            var template = GameObject.Find("logoImage");
            TOHELogo = Object.Instantiate(template, __instance.CreditsParent.transform);
            TOHELogo.name = "TOHE Logo Image";
            TOHELogo.transform.localPosition = template.transform.localPosition;
            var logoRenderer = TOHELogo.GetComponent<SpriteRenderer>();
            logoRenderer.sprite = Utils.LoadSprite("TOHE.Resources.Images.TOHE-Icon.png", 76f);

            GameObject CreateBlock(string header, List<string> lines)
            {
                var template = __instance.CreditsParent.transform.GetChild(2).gameObject;
                var block = Object.Instantiate(template, __instance.CreditsParent.transform);
                block.name = $"TOHE CreditsBlock";
                string content = "";
                lines.Do(l => content += "\n" + l);
                block.transform.FindChild("Header").GetComponent<TextMeshPro>().SetText(header);
                var tmp = block.transform.FindChild("CreditsLines").GetComponent<TextMeshPro>();
                tmp.fontSizeMax = tmp.fontSizeMin = 1.9f;
                tmp.SetText(content.TrimStart('\n'));
                return block;
            }

            float offset = 1f;

            var Devs = CreateBlock("", devsList);
            Devs.transform.localPosition = TOHELogo.transform.localPosition - new Vector3(0f, offset, 0f);
            offset += 0.35f * devsList.Count + 1f;

            var Translators = CreateBlock(GetString("Translator"), translatorsList);
            Translators.transform.localPosition = TOHELogo.transform.localPosition - new Vector3(0f, offset, 0f);
            offset += 0.35f * translatorsList.Count + 1f;

            var Acknowledgements = CreateBlock(GetString("Acknowledgement"), acList);
            Acknowledgements.transform.localPosition = TOHELogo.transform.localPosition - new Vector3(0f, offset, 0f);
            offset += 0.35f * acList.Count + 3f;

            __instance.CreditsParent.ForEachChild((Il2CppSystem.Action<GameObject>)Move);
            void Move(GameObject obj)
            {
                if (obj.name.StartsWith("TOHE")) return;
                obj.transform.localPosition -= new Vector3(0f, offset, 0f);
            }

        }
    }
}