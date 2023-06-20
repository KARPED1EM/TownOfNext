using HarmonyLib;
using TOHE.Roles.Core;
using TOHE.Roles.Core.Interfaces;
using UnityEngine;

namespace TOHE;

public static class CustomButton
{
    public static Sprite Get(string name) => Utils.LoadSprite($"TOHE.Resources.Images.Skills.{name}.png", 115f);
}

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update)), HarmonyPriority(Priority.LowerThanNormal)]
public static class HudSpritePatch
{
    private static Sprite Kill;
    private static Sprite Ability;
    private static Sprite Vent;
    private static Sprite Report;
    public static void Postfix(HudManager __instance)
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null || !GameStates.IsModHost) return;
        if (!SetHudActivePatch.IsActive || !player.IsAlive()) return;
        if (!AmongUsClient.Instance.IsGameStarted || !Main.introDestroyed)
        {
            Kill = null;
            Ability = null;
            Vent = null;
            return;
        }

        bool shapeshifting = Main.CheckShapeshift.TryGetValue(player.PlayerId, out bool ss) && ss;

        Kill ??= __instance.KillButton.graphic.sprite;
        Ability ??= __instance.AbilityButton.graphic.sprite;
        Vent ??= __instance.ImpostorVentButton.graphic.sprite;
        Report ??= __instance.ReportButton.graphic.sprite;

        Sprite newKillButton = Kill;
        Sprite newAbilityButton = Ability;
        Sprite newVentButton = Vent;
        Sprite newReportButton = Report;

        if (Main.EnableCustomButton.Value)
        {

            if (player.GetRoleClass() is IKiller killer)
            {
                if (killer.OverrideKillButtonSprite(out var newKillButtonName))
                    newKillButton = CustomButton.Get(newKillButtonName);

                if (killer.OverrideVentButtonSprite(out var newVentButtonName))
                    newVentButton = CustomButton.Get(newVentButtonName);
            }

            if (player.GetRoleClass().OverrideAbilityButtonSprite(out var newAbilityButtonName))
                newAbilityButton = CustomButton.Get(newAbilityButtonName);

            if (player.GetRoleClass().OverrideReportButtonSprite(out var newReportButtonName))
                newReportButton = CustomButton.Get(newReportButtonName);
        }

        if (player.GetRoleClass() is IKiller)
        {
            __instance.KillButton.graphic.sprite = newKillButton;
            __instance.ImpostorVentButton.graphic.sprite = newVentButton;
        }

        __instance.AbilityButton.graphic.sprite = newAbilityButton;
        __instance.ReportButton.graphic.sprite = newReportButton;
    }
}