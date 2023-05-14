using HarmonyLib;
using TOHE.Roles.Impostor;
using UnityEngine;

namespace TOHE;

public static class CustomButton
{
    public static Sprite Target = Utils.LoadSprite("TOHE.Resources.Images.Skills.Target.png", 115f);
    public static Sprite Judge = Utils.LoadSprite("TOHE.Resources.Images.Skills.Judge.png", 115f);
    public static Sprite Assassinate = Utils.LoadSprite("TOHE.Resources.Images.Skills.Assassinate.png", 115f);
    public static Sprite Mark = Utils.LoadSprite("TOHE.Resources.Images.Skills.Mark.png", 115f);
    public static Sprite Boom = Utils.LoadSprite("TOHE.Resources.Images.Skills.Boom.png", 115f);
    public static Sprite Camo = Utils.LoadSprite("TOHE.Resources.Images.Skills.Camo.png", 115f);
    public static Sprite Douse = Utils.LoadSprite("TOHE.Resources.Images.Skills.Douse.png", 115f);
    public static Sprite Ignite = Utils.LoadSprite("TOHE.Resources.Images.Skills.Ignite.png", 115f);
    public static Sprite FireworkD = Utils.LoadSprite("TOHE.Resources.Images.Skills.FireworkD.png", 115f);
    public static Sprite FireworkP = Utils.LoadSprite("TOHE.Resources.Images.Skills.FireworkP.png", 115f);
    public static Sprite Hack = Utils.LoadSprite("TOHE.Resources.Images.Skills.Hack.png", 115f);
    public static Sprite Hangman = Utils.LoadSprite("TOHE.Resources.Images.Skills.Hangman.png", 115f);
    public static Sprite Paranoid = Utils.LoadSprite("TOHE.Resources.Images.Skills.Paranoid.png", 115f);
    public static Sprite Puttpuer = Utils.LoadSprite("TOHE.Resources.Images.Skills.Puttpuer.png", 115f);
    public static Sprite Shield = Utils.LoadSprite("TOHE.Resources.Images.Skills.Shield.png", 115f);
    public static Sprite Sidekick = Utils.LoadSprite("TOHE.Resources.Images.Skills.Sidekick.png", 115f);
    public static Sprite Subbus = Utils.LoadSprite("TOHE.Resources.Images.Skills.Subbus.png", 115f);
    public static Sprite Suidce = Utils.LoadSprite("TOHE.Resources.Images.Skills.Suidce.png", 115f);
    public static Sprite Track = Utils.LoadSprite("TOHE.Resources.Images.Skills.Track.png", 115f);
    public static Sprite Bite = Utils.LoadSprite("TOHE.Resources.Images.Skills.Bite.png", 115f);
    public static Sprite Veteran = Utils.LoadSprite("TOHE.Resources.Images.Skills.Veteran.png", 115f);
    public static Sprite Vulture = Utils.LoadSprite("TOHE.Resources.Images.Skills.Vulture.png", 115f);
}

[HarmonyPriority(520)]
[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class HudSpritePatch
{
    private static Sprite Assassinate => CustomButton.Assassinate;
    private static Sprite Mark => CustomButton.Mark;
    private static Sprite Boom => CustomButton.Boom;
    private static Sprite Camo => CustomButton.Camo;
    private static Sprite Douse => CustomButton.Douse;
    private static Sprite Ignite => CustomButton.Ignite;
    private static Sprite FireworkD => CustomButton.FireworkD;
    private static Sprite FireworkP => CustomButton.FireworkP;
    private static Sprite Hack => CustomButton.Hack;
    private static Sprite Hangman => CustomButton.Hangman;
    private static Sprite Paranoid => CustomButton.Paranoid;
    private static Sprite Puttpuer => CustomButton.Puttpuer;
    private static Sprite Shield => CustomButton.Shield;
    private static Sprite Sidekick => CustomButton.Sidekick;
    private static Sprite Subbus => CustomButton.Subbus;
    private static Sprite Suidce => CustomButton.Suidce;
    private static Sprite Track => CustomButton.Track;
    private static Sprite Bite => CustomButton.Bite;
    private static Sprite Veteran => CustomButton.Veteran;
    private static Sprite Vulture => CustomButton.Vulture;

    public static void Postfix(HudManager __instance)
    {
        if (!GameStates.IsModHost) return;
        var player = PlayerControl.LocalPlayer;
        if (player == null) return;

        if (!AmongUsClient.Instance.IsGameStarted) return;
        if (!SetHudActivePatch.IsActive || !player.IsAlive()) return;

        bool shapeshifting = Main.CheckShapeshift.TryGetValue(player.PlayerId, out bool ss) && ss;

        Sprite Kill = __instance.KillButton.graphic.sprite;
        Sprite Ability = __instance.AbilityButton.graphic.sprite;

        Logger.Test("Override Sprite");

        switch (player.GetCustomRole())
        {
            case CustomRoles.Assassin:
                if (!shapeshifting)
                {
                    __instance.KillButton.graphic.sprite = Mark;
                    if (Assassin.MarkedPlayer.ContainsKey(player.PlayerId))
                        __instance.AbilityButton.graphic.sprite = Assassinate;
                }
                break;
            case CustomRoles.Bomber:
                __instance.AbilityButton.graphic.sprite = Boom;
                break;
            case CustomRoles.Concealer:
                __instance.AbilityButton.graphic.sprite = Camo;
                break;
            case CustomRoles.Arsonist:
                __instance.KillButton.graphic.sprite = Douse;
                if (player.IsDouseDone()) __instance.ImpostorVentButton.graphic.sprite = Ignite;
                break;
            case CustomRoles.FireWorks:
                if (FireWorks.nowFireWorksCount[player.PlayerId] == 0)
                    __instance.AbilityButton.graphic.sprite = FireworkD;
                else
                    __instance.AbilityButton.graphic.sprite = FireworkP;
                break;
            case CustomRoles.Hacker:
                __instance.AbilityButton.graphic.sprite = Hack;
                break;
            case CustomRoles.Hangman:
                if (shapeshifting) __instance.AbilityButton.graphic.sprite = Hangman;
                break;
            case CustomRoles.Paranoia:
                __instance.AbilityButton.graphic.sprite = Paranoid;
                break;
            case CustomRoles.Puppeteer:
                __instance.KillButton.graphic.sprite = Puttpuer;
                break;
            case CustomRoles.Medicaler:
                __instance.KillButton.graphic.sprite = Shield;
                break;
            case CustomRoles.Gangster:
                if (Gangster.CanRecruit(player.PlayerId)) __instance.KillButton.graphic.sprite = Sidekick;
                break;
            case CustomRoles.Succubus:
                __instance.KillButton.graphic.sprite = Subbus;
                break;
            case CustomRoles.Innocent:
                __instance.KillButton.graphic.sprite = Suidce;
                break;
            case CustomRoles.EvilTracker:
                __instance.AbilityButton.graphic.sprite = Track;
                break;
            case CustomRoles.Vampire:
                __instance.KillButton.graphic.sprite = Bite;
                break;
            case CustomRoles.Veteran:
                __instance.AbilityButton.graphic.sprite = Veteran;
                break;
            case CustomRoles.Pelican:
                __instance.KillButton.graphic.sprite = Vulture;
                break;
        }
    }
}
