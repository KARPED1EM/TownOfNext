using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TOHE.Roles.Impostor;
using UnityEngine;

namespace TOHE;

public static class CustomButton
{
    public static Sprite Target = Utils.LoadSprite("TOHE.Resources.Images.Skills.Target.png", 115f);
    public static Sprite Judge = Utils.LoadSprite("TOHE.Resources.Images.Skills.Judge.png", 115f);
    public static Sprite Assassinate = Utils.LoadSprite("TOHE.Resources.Images.Skills.Assassinate.png", 115f);
    public static Sprite Mark = Utils.LoadSprite("TOHE.Resources.Images.Skills.Mark.png", 115f);
}

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class HudSpritePatch
{
    private static Sprite Assassinate => CustomButton.Assassinate;
    private static Sprite Mark => CustomButton.Mark;

    public static void Postfix(HudManager __instance)
    {
        if (!GameStates.IsModHost) return;
        var player = PlayerControl.LocalPlayer;
        if (player == null) return;

        if (!AmongUsClient.Instance.IsGameStarted) return;
        if (!SetHudActivePatch.IsActive || !player.IsAlive()) return;

        Sprite Kill = __instance.KillButton.graphic.sprite;
        Sprite Ability = __instance.AbilityButton.graphic.sprite;

        switch (player.GetCustomRole())
        {
            case CustomRoles.Assassin:
                if (!Main.CheckShapeshift.TryGetValue(player.PlayerId, out bool ss) || !ss)
                {
                    __instance.KillButton.graphic.sprite = Mark;
                    if (Assassin.MarkedPlayer.ContainsKey(player.PlayerId))
                        __instance.AbilityButton.graphic.sprite = Assassinate;
                }
                break;
        }
    }
}
