using HarmonyLib;
using UnityEngine;

namespace TONX;

[HarmonyPatch]
public class HelloweenButtonPatch
{
    public static bool isEnabled = false;
    public static GameObject HelloweenButton;
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Awake)), HarmonyPostfix]
    public static void ShipStatusFixedUpdate(ShipStatus __instance)
    {
        if (Main.NormalOptions.MapId != 0) return;
        if (HelloweenButton == null)
        {
            var template = __instance.EmergencyButton.gameObject;
            HelloweenButton = Object.Instantiate(template, template.transform.parent);
            HelloweenButton.name = "Switch Helloween Button";
            HelloweenButton.transform.localScale = new Vector3(0.65f, 0.65f, 1f);
            HelloweenButton.transform.localPosition = new Vector3(-9.57f, -5.36f, -10f);
            var console = HelloweenButton.GetComponent<SystemConsole>();
            console.Image.color = new Color32(80, 255, 255, byte.MaxValue);
            console.usableDistance /= 2;
            console.name = "HelloweenConsole";
        }
    }
    [HarmonyPatch(typeof(SystemConsole), nameof(SystemConsole.Use)), HarmonyPrefix]
    public static bool UseConsole(SystemConsole __instance)
    {
        if (__instance.name != "HelloweenConsole") return true;
        isEnabled = !isEnabled;
        ShipStatus.Instance.gameObject.transform.FindChild("Helloween")?.gameObject.SetActive(isEnabled);
        RPC.PlaySoundRPC(PlayerControl.LocalPlayer.PlayerId, isEnabled ? Sounds.ImpTransform : Sounds.TaskUpdateSound);
        return false;
    }
}