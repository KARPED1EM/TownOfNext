using HarmonyLib;
using UnityEngine;

namespace TOHE.Patches;

[HarmonyPatch(typeof(ButtonRolloverHandler))]
class ButtonRolloverHandlerPatch
{
    [HarmonyPatch(nameof(ButtonRolloverHandler.DoMouseOver)), HarmonyPrefix]
    public static void DoMouseOver_Prefix(ButtonRolloverHandler __instance)
    {
        if (__instance.OverColor == new Color(0, 1, 0, 1))
            __instance.OverColor = new Color32(255, 192, 203, 255);
    }
    [HarmonyPatch(nameof(ButtonRolloverHandler.ChangeOutColor)), HarmonyPrefix]
    public static void ChangeOutColor_Prefix(ButtonRolloverHandler __instance, ref Color color)
    {
        if (color == new Color(0, 1, 0.165f, 1))
        {
            color = new Color32(255, 192, 203, 180);
            Logger.Test("Changed");
        }
    }
}