using HarmonyLib;
using System.Collections.Generic;

namespace TOHE.Patches;

[HarmonyPatch(typeof(ChatBubble))]
public static class ChatBubblePatch
{
    internal static readonly Queue<int> SetLeftQueue = new();
    [HarmonyPatch(nameof(ChatBubble.SetRight)), HarmonyPostfix]
    public static void SetRight_Postfix(ChatBubble __instance)
    {
        if (Main.isChatCommand) __instance.SetLeft();
        __instance.TextArea.richText = true;
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
}