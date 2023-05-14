using HarmonyLib;
using TOHE.Roles.Impostor;
using UnityEngine;

namespace TOHE;

public static class CustomButton
{
    public static void Init()
    {
        Target = Utils.LoadSprite("TOHE.Resources.Images.Skills.Target.png", 115f);
        Judge = Utils.LoadSprite("TOHE.Resources.Images.Skills.Judge.png", 115f);
        Assassinate = Utils.LoadSprite("TOHE.Resources.Images.Skills.Assassinate.png", 115f);
        Mark = Utils.LoadSprite("TOHE.Resources.Images.Skills.Mark.png", 115f);
        Boom = Utils.LoadSprite("TOHE.Resources.Images.Skills.Boom.png", 115f);
        Camo = Utils.LoadSprite("TOHE.Resources.Images.Skills.Camo.png", 115f);
        Douse = Utils.LoadSprite("TOHE.Resources.Images.Skills.Douse.png", 115f);
        Ignite = Utils.LoadSprite("TOHE.Resources.Images.Skills.Ignite.png", 115f);
        FireworkD = Utils.LoadSprite("TOHE.Resources.Images.Skills.FireworkD.png", 115f);
        FireworkP = Utils.LoadSprite("TOHE.Resources.Images.Skills.FireworkP.png", 115f);
        Hack = Utils.LoadSprite("TOHE.Resources.Images.Skills.Hack.png", 115f);
        Hangman = Utils.LoadSprite("TOHE.Resources.Images.Skills.Hangman.png", 115f);
        Paranoid = Utils.LoadSprite("TOHE.Resources.Images.Skills.Paranoid.png", 115f);
        Puttpuer = Utils.LoadSprite("TOHE.Resources.Images.Skills.Puttpuer.png", 115f);
        Shield = Utils.LoadSprite("TOHE.Resources.Images.Skills.Shield.png", 115f);
        Sidekick = Utils.LoadSprite("TOHE.Resources.Images.Skills.Sidekick.png", 115f);
        Subbus = Utils.LoadSprite("TOHE.Resources.Images.Skills.Subbus.png", 115f);
        Suidce = Utils.LoadSprite("TOHE.Resources.Images.Skills.Suidce.png", 115f);
        Track = Utils.LoadSprite("TOHE.Resources.Images.Skills.Track.png", 115f);
        Bite = Utils.LoadSprite("TOHE.Resources.Images.Skills.Bite.png", 115f);
        Veteran = Utils.LoadSprite("TOHE.Resources.Images.Skills.Veteran.png", 115f);
        Vulture = Utils.LoadSprite("TOHE.Resources.Images.Skills.Vulture.png", 115f);
    }

    public static Sprite Target;
    public static Sprite Judge;
    public static Sprite Assassinate;
    public static Sprite Mark;
    public static Sprite Boom;
    public static Sprite Camo;
    public static Sprite Douse;
    public static Sprite Ignite;
    public static Sprite FireworkD;
    public static Sprite FireworkP;
    public static Sprite Hack;
    public static Sprite Hangman;
    public static Sprite Paranoid;
    public static Sprite Puttpuer;
    public static Sprite Shield;
    public static Sprite Sidekick;
    public static Sprite Subbus;
    public static Sprite Suidce;
    public static Sprite Track;
    public static Sprite Bite;
    public static Sprite Veteran;
    public static Sprite Vulture;

}

[HarmonyPriority(520)]
[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class HudSpritePatch
{
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

        

        switch (player.GetCustomRole())
        {
            case CustomRoles.Assassin:
                if (!shapeshifting)
                {
                    __instance.KillButton.graphic.sprite = CustomButton.Mark;
                    if (Assassin.MarkedPlayer.ContainsKey(player.PlayerId))
                        __instance.AbilityButton.graphic.sprite = CustomButton.Assassinate;
                }
                break;
            case CustomRoles.Bomber:
                __instance.AbilityButton.graphic.sprite = CustomButton.Boom;
                break;
            case CustomRoles.Concealer:
                __instance.AbilityButton.graphic.sprite = CustomButton.Camo;
                break;
            case CustomRoles.Arsonist:
                __instance.KillButton.graphic.sprite = CustomButton.Douse;
                if (player.IsDouseDone()) __instance.ImpostorVentButton.graphic.sprite = CustomButton.Ignite;
                break;
            case CustomRoles.FireWorks:
                if (FireWorks.nowFireWorksCount[player.PlayerId] == 0)
                    __instance.AbilityButton.graphic.sprite = CustomButton.FireworkD;
                else
                    __instance.AbilityButton.graphic.sprite = CustomButton.FireworkP;
                break;
            case CustomRoles.Hacker:
                __instance.AbilityButton.graphic.sprite = CustomButton.Hack;
                break;
            case CustomRoles.Hangman:
                if (shapeshifting) __instance.AbilityButton.graphic.sprite = CustomButton.Hangman;
                break;
            case CustomRoles.Paranoia:
                __instance.AbilityButton.graphic.sprite = CustomButton.Paranoid;
                break;
            case CustomRoles.Puppeteer:
                __instance.KillButton.graphic.sprite = CustomButton.Puttpuer;
                break;
            case CustomRoles.Medicaler:
                __instance.KillButton.graphic.sprite = CustomButton.Shield;
                break;
            case CustomRoles.Gangster:
                if (Gangster.CanRecruit(player.PlayerId)) __instance.KillButton.graphic.sprite = CustomButton.Sidekick;
                break;
            case CustomRoles.Succubus:
                __instance.KillButton.graphic.sprite = CustomButton.Subbus;
                break;
            case CustomRoles.Innocent:
                __instance.KillButton.graphic.sprite = CustomButton.Suidce;
                break;
            case CustomRoles.EvilTracker:
                __instance.AbilityButton.graphic.sprite = CustomButton.Track;
                break;
            case CustomRoles.Vampire:
                __instance.KillButton.graphic.sprite = CustomButton.Bite;
                break;
            case CustomRoles.Veteran:
                __instance.AbilityButton.graphic.sprite = CustomButton.Veteran;
                break;
            case CustomRoles.Pelican:
                __instance.KillButton.graphic.sprite = CustomButton.Vulture;
                break;
        }
    }
}
