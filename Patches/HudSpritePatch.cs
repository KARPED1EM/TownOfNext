using HarmonyLib;
using TOHE.Modules;
using TOHE.Roles.Impostor;
using UnityEngine;

namespace TOHE;

public static class CustomButton
{
    public static Sprite Target = Utils.LoadSprite("TOHE.Resources.Images.Skills.Target.png", 115f);
    public static Sprite Judge = Utils.LoadSprite("TOHE.Resources.Images.Skills.Judge.png", 115f);
    public static Sprite Assassinate = Utils.LoadSprite("TOHE.Resources.Images.Skills.Assassinate.png", 115f);
    public static Sprite Mark = Utils.LoadSprite("TOHE.Resources.Images.Skills.Mark.png", 115f);
    public static Sprite Bomb = Utils.LoadSprite("TOHE.Resources.Images.Skills.Bomb.png", 115f);
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
    public static Sprite Kill;
    public static Sprite Ability;
    public static Sprite Vent;
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

        if (!Kill) Kill = __instance.KillButton.graphic.sprite;
        if (!Ability) Ability = __instance.AbilityButton.graphic.sprite;
        if (!Vent) Vent = __instance.ImpostorVentButton.graphic.sprite;

        Sprite newKillButton = Kill;
        Sprite newAbilityButton = Ability;
        Sprite newVentButton = Vent;

        if (!Main.EnableCustomButton.Value) goto EndOfSelectImg;

        switch (player.GetCustomRole())
        {
            case CustomRoles.Assassin:
                if (!shapeshifting)
                {
                    newKillButton = CustomButton.Mark;
                    if (Assassin.MarkedPlayer.ContainsKey(player.PlayerId))
                        newAbilityButton = CustomButton.Assassinate;
                }
                break;
            case CustomRoles.Bomber:
                newAbilityButton = CustomButton.Bomb;
                break;
            case CustomRoles.Concealer:
                newAbilityButton = CustomButton.Camo;
                break;
            case CustomRoles.Arsonist:
                newKillButton = CustomButton.Douse;
                if (player.IsDouseDone()) newVentButton = CustomButton.Ignite;
                break;
            case CustomRoles.FireWorks:
                if (FireWorks.nowFireWorksCount[player.PlayerId] == 0)
                    newAbilityButton = CustomButton.FireworkD;
                else
                    newAbilityButton = CustomButton.FireworkP;
                break;
            case CustomRoles.Hacker:
                newAbilityButton = CustomButton.Hack;
                break;
            case CustomRoles.Hangman:
                if (shapeshifting) newAbilityButton = CustomButton.Hangman;
                break;
            case CustomRoles.Paranoia:
                newAbilityButton = CustomButton.Paranoid;
                break;
            case CustomRoles.Puppeteer:
                newKillButton = CustomButton.Puttpuer;
                break;
            case CustomRoles.Medicaler:
                newKillButton = CustomButton.Shield;
                break;
            case CustomRoles.Gangster:
                if (Gangster.CanRecruit(player.PlayerId)) newKillButton = CustomButton.Sidekick;
                break;
            case CustomRoles.Succubus:
                newKillButton = CustomButton.Subbus;
                break;
            case CustomRoles.Innocent:
                newKillButton = CustomButton.Suidce;
                break;
            case CustomRoles.EvilTracker:
                newAbilityButton = CustomButton.Track;
                break;
            case CustomRoles.Vampire:
                newKillButton = CustomButton.Bite;
                break;
            case CustomRoles.Veteran:
                newAbilityButton = CustomButton.Veteran;
                break;
            case CustomRoles.Pelican:
                newKillButton = CustomButton.Vulture;
                break;
        }

    EndOfSelectImg:

        __instance.KillButton.graphic.sprite = newKillButton;
        __instance.AbilityButton.graphic.sprite = newAbilityButton;
        __instance.ImpostorVentButton.graphic.sprite = newVentButton;

    }
}
