using HarmonyLib;
using System;
using UnityEngine;

namespace TOHE.Patches;

[HarmonyPatch(typeof(RegionMenu))]
public static class RegionMenuPatch
{
    public static Scroller Scroller;

    [HarmonyPatch(nameof(RegionMenu.Awake))]
    public static void Postfix(RegionMenu __instance)
    {
        if (Scroller != null) return;
        var Inner = new GameObject("Inner");
        __instance.ButtonPool.gameObject.ForEachChild(new Action<GameObject>(c =>
        {
            if (c.name != "Backdrop")
            c.transform.SetParent(Inner.transform);
        }));
        Scroller = __instance.ButtonPool.transform.parent.gameObject.AddComponent<Scroller>();
        Scroller.Inner = Inner.transform;
        Scroller.MouseMustBeOverToScroll = true;
        Scroller.ClickMask = __instance.ButtonPool.transform.FindChild("Backdrop").GetComponent<BoxCollider2D>();
        Scroller.ScrollWheelSpeed = 0.7f;
        Scroller.allowY = true;
    }
}