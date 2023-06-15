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
                $"<color={Main.ModColor}>♡KARPED1EM</color> - {GetString("Creater")}",
                $"<color={Main.ModColor}>♡IRIDESCENT</color> - {GetString("Art")}",
                $"SHAAARKY - {GetString("Developer")}",
                $"Endrmen40409 - {GetString("Art")}",
                $"天寸梦初 - {GetString("Developer")}",
                $"NCSIMON - {GetString("Developer")}",
                $"Commandf1 - {GetString("Developer")}",
                $"喜 - {GetString("Developer")}",
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
                $"罗寄 - {GetString("Sponsor")}",
                $"鬼 - {GetString("Sponsor")}",
                $"喜 - {GetString("Sponsor")}",
                $"小叨院长 - {GetString("Sponsor")}",
                $"波奇酱 - {GetString("Sponsor")}",
                $"法师 - {GetString("Sponsor")}",
                $"沐煊 - {GetString("Sponsor")}",
                $"SolarFlare - {GetString("Sponsor")}",
                $"林林林 - {GetString("Sponsor")}",
                $"撒币 - {GetString("Sponsor")}",
                $"斯卡蒂Skadi - {GetString("Sponsor")}",
                $"ltemten - {GetString("Sponsor")}",
                $"Night_瓜 - {GetString("Sponsor")}",
                $"群诱饵 - {GetString("Sponsor")}",
                $"Slok - {GetString("Sponsor")}",
                $"辣鸡 - {GetString("Sponsor")}",
                $"湛蓝色 - {GetString("Sponsor")}",
                $"小黄117 - {GetString("Sponsor")}",
                $"chun - {GetString("Sponsor")}",
                $"Z某 - {GetString("Sponsor")}",
                $"Shark - {GetString("Sponsor")}",
                $"清风awa - {GetString("Sponsor")}",
                $"1 1 1 1 - {GetString("Sponsor")}",

                $"bunny - {GetString("Booster")}",
                $"Loonie - {GetString("Booster")}",
                $"Namra - {GetString("Booster")}",
                $"KNIGHT - {GetString("Booster")}",
                $"SolarFlare - {GetString("Booster")}",
                $"Bluéfôx. - {GetString("Booster")}",
                $"shiftyrose - {GetString("Booster")}",
                $"M ™ - {GetString("Booster")}",
                $"yunfi - {GetString("Booster")}",
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