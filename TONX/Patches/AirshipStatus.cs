using HarmonyLib;

using TONX.Roles.Core;

namespace TONX;

//参考元:https://github.com/yukieiji/ExtremeRoles/blob/master/ExtremeRoles/Patches/AirShipStatusPatch.cs
[HarmonyPatch(typeof(AirshipStatus), nameof(AirshipStatus.PrespawnStep))]
public static class AirshipStatusPrespawnStepPatch
{
    public static bool Prefix()
    {
        return !PlayerControl.LocalPlayer.Is(CustomRoles.GM); // GMは湧き画面をスキップ
    }
}