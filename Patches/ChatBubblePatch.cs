using HarmonyLib;
using UnityEngine;

namespace TOHE.Patches;

[HarmonyPatch(typeof(ChatBubble))]
public static class ChatBubblePatch
{
#nullable enable
    private static bool IsModdedMsg(string? name) => name?.RemoveHtmlTags() != name;
#nullable disable
    [HarmonyPatch(nameof(ChatBubble.SetRight)), HarmonyPostfix]
    public static void SetRight_Postfix(ChatBubble __instance)
    {
        __instance.TextArea.richText = true;
        if (Main.isChatCommand) __instance.SetLeft();
    }
    [HarmonyPatch(nameof(ChatBubble.SetLeft)), HarmonyPostfix]
    public static void SetLeft_Postfix(ChatBubble __instance)
    {
        __instance.TextArea.richText = true;
    }
    [HarmonyPatch(nameof(ChatBubble.SetName)), HarmonyPostfix]
    public static void SetName_Postfix(ChatBubble __instance)
    {
        if (GameStates.IsInGame && __instance.playerInfo.PlayerId == PlayerControl.LocalPlayer.PlayerId)
            __instance.NameText.color = PlayerControl.LocalPlayer.GetRoleColor();
    }
    [HarmonyPatch(nameof(ChatBubble.SetText)), HarmonyPrefix]
    public static void SetText_Prefix(ChatBubble __instance, ref string chatText)
    {
        bool modded = IsModdedMsg(__instance.playerInfo.PlayerName);
        var sr = __instance.transform.FindChild("Background").GetComponent<SpriteRenderer>();
        sr.color = modded ? new Color(0, 0, 0) : new Color(1, 1, 1);
        if (modded)
        {
            chatText = Utils.ColorString(Color.white, chatText);
            var newOutfit = Camouflage.CamouflageOutfit_KPD;
            __instance.Player.SetBodyColor(newOutfit.ColorId);
            __instance.Player.SetHat(newOutfit.HatId, newOutfit.ColorId);
            __instance.Player.SetSkin(newOutfit.SkinId, newOutfit.ColorId);
            __instance.Player.SetVisor(newOutfit.VisorId, newOutfit.ColorId);
        }
    }
}