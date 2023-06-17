using AmongUs.Data;
using HarmonyLib;
using System;
using TMPro;
using UnityEngine;

namespace TOHE;

[HarmonyPatch(typeof(ChatController))]
public static class SendTargetPatch
{
    public enum SendTargets
    {
        Default,
        All,
        Dead
    }
    public static SendTargets SendTarget = SendTargets.Default;
    public static GameObject SendTargetShower;
    [HarmonyPatch(nameof(ChatController.Awake)), HarmonyPostfix]
    public static void Awake_Postfix(ChatController __instance)
    {
        __instance.CharCount.SetText("");
        if (SendTargetShower != null) return;
        SendTargetShower = UnityEngine.Object.Instantiate(__instance.CharCount.gameObject, __instance.CharCount.transform.parent);
        SendTargetShower.name = "TOHE Send Target Shower";
        SendTargetShower.transform.localPosition = new Vector3(1.95f, 0.5f, 0f);
        SendTargetShower.GetComponent<RectTransform>().sizeDelta = new Vector2(5f, 0.1f);
        var tmp = SendTargetShower.GetComponent<TextMeshPro>();
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.outlineWidth = 1f;
    }
    [HarmonyPatch(nameof(ChatController.Update)), HarmonyPostfix]
    public static void Update_Postfix(ChatController __instance)
    {
        if (SendTargetShower == null) return;
        string text = Translator.GetString($"SendTargets.{Enum.GetName(SendTarget)}");
        if (AmongUsClient.Instance.AmHost && GameStates.IsInGame && __instance.IsOpen)
        {
            text += "<size=75%>" + Translator.GetString("SendTargetSwitchNotice") + "</size>";
            if (Input.GetKey(KeyCode.LeftShift)) SendTarget = SendTargets.All;
            else if (Input.GetKey(KeyCode.LeftControl)) SendTarget = SendTargets.Dead;
            else SendTarget = SendTargets.Default;
        }
        else SendTarget = SendTargets.Default;
        SendTargetShower?.GetComponent<TextMeshPro>()?.SetText(text);
    }
}

[HarmonyPatch(typeof(ChatController), nameof(ChatController.Update))]
public static class ChatControllerUpdatePatch
{
    public static int CurrentHistorySelection = -1;
    public static void Prefix()
    {
        if (AmongUsClient.Instance.AmHost && DataManager.Settings.Multiplayer.ChatMode == InnerNet.QuickChatModes.QuickChatOnly)
            DataManager.Settings.Multiplayer.ChatMode = InnerNet.QuickChatModes.FreeChatOrQuickChat; //コマンドを打つためにホストのみ常時フリーチャット開放
    }
    public static void Postfix(ChatController __instance)
    {
        if (!__instance.TextArea.hasFocus) return;
        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.C))
            ClipboardHelper.PutClipboardString(__instance.TextArea.text);
        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.V))
            __instance.TextArea.SetText(__instance.TextArea.text + GUIUtility.systemCopyBuffer);
        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.X))
        {
            ClipboardHelper.PutClipboardString(__instance.TextArea.text);
            __instance.TextArea.SetText("");
        }
        if (Input.GetKeyDown(KeyCode.UpArrow) && ChatCommands.ChatHistory.Count > 0)
        {
            CurrentHistorySelection = Mathf.Clamp(--CurrentHistorySelection, 0, ChatCommands.ChatHistory.Count - 1);
            __instance.TextArea.SetText(ChatCommands.ChatHistory[CurrentHistorySelection]);
        }
        if (Input.GetKeyDown(KeyCode.DownArrow) && ChatCommands.ChatHistory.Count > 0)
        {
            CurrentHistorySelection++;
            if (CurrentHistorySelection < ChatCommands.ChatHistory.Count)
                __instance.TextArea.SetText(ChatCommands.ChatHistory[CurrentHistorySelection]);
            else __instance.TextArea.SetText("");
        }
    }
}