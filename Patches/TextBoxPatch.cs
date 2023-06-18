using HarmonyLib;

namespace TOHE;

[HarmonyPatch(typeof(TextBoxTMP))]
public class TextBoxPatch
{
    //[HarmonyPatch(nameof(TextBoxTMP.IsCharAllowed)), HarmonyPostfix]
    //public static void IsCharAllowed(TextBoxTMP __instance, char i, ref bool __result) => __result &= i is not ('\r' or '\n');
    [HarmonyPatch(nameof(TextBoxTMP.SetText)), HarmonyPrefix]
    public static void ModifyCharacterLimit(TextBoxTMP __instance) => __instance.characterLimit = AmongUsClient.Instance.AmHost ? 999 : 300;
}