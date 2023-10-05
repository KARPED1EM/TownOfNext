using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using TONX.Roles.AddOns.Common;
using TONX.Roles.Core;
using UnityEngine;

namespace TONX;

[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.FixedUpdate))]
class ShipFixedUpdatePatch
{
    public static void Postfix(ShipStatus __instance)
    {
        //ここより上、全員が実行する
        if (!AmongUsClient.Instance.AmHost) return;
        //ここより下、ホストのみが実行する
    }
}
[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.RepairSystem))]
class RepairSystemPatch
{
    public static bool Prefix(ShipStatus __instance,
        [HarmonyArgument(0)] SystemTypes systemType,
        [HarmonyArgument(1)] PlayerControl player,
        [HarmonyArgument(2)] byte amount)
    {
        if (systemType == SystemTypes.Sabotage)
        {
            Logger.Info("SystemType: " + systemType.ToString() + ", PlayerName: " + player.GetNameWithRole() + ", SabotageType: " + (SystemTypes)amount, "RepairSystem");
        }
        else
        {
            Logger.Info("SystemType: " + systemType.ToString() + ", PlayerName: " + player.GetNameWithRole() + ", amount: " + amount, "RepairSystem");
        }

        if (RepairSender.enabled && AmongUsClient.Instance.NetworkMode != NetworkModes.OnlineGame)
        {
            Logger.SendInGame("SystemType: " + systemType.ToString() + ", PlayerName: " + player.GetNameWithRole() + ", amount: " + amount);
        }

        if (!AmongUsClient.Instance.AmHost) return true; //以下、ホストのみ実行

        if (!player.Is(CustomRoleTypes.Impostor) && player.Is(CustomRoles.Fool))
        {
            if (systemType is SystemTypes.Reactor or SystemTypes.Comms or SystemTypes.Electrical or SystemTypes.Laboratory or SystemTypes.LifeSupp) return false;
            if (systemType is SystemTypes.Doors && Fool.OptionImpFoolCanNotOpenDoor.GetBool()) return false;
        }

        if (systemType == SystemTypes.Sabotage)
        {
            if (player.Is(CustomRoleTypes.Impostor) && player.Is(CustomRoles.Fool) && Fool.OptionImpFoolCanNotSabotage.GetBool())
                return false;
            if (Options.DisableSabotage.GetBool()) return false;
            var nextSabotage = (SystemTypes)amount;
            //PVP禁止破坏
            if ((Options.CurrentGameMode == CustomGameMode.SoloKombat)) return false;
            var roleClass = player.GetRoleClass();
            if (roleClass != null)
            {
                return roleClass.OnInvokeSabotage(nextSabotage);
            }
            else
            {
                return CanSabotage(player, nextSabotage);
            }
        }
        // カメラ無効時，バニラプレイヤーはカメラを開けるので点滅させない
        else if (systemType == SystemTypes.Security && amount == 1)
        {
            var camerasDisabled = (MapNames)Main.NormalOptions.MapId switch
            {
                MapNames.Skeld => Options.DisableSkeldCamera.GetBool(),
                MapNames.Polus => Options.DisablePolusCamera.GetBool(),
                MapNames.Airship => Options.DisableAirshipCamera.GetBool(),
                _ => false,
            };
            return !camerasDisabled;
        }
        else
        {
            return CustomRoleManager.OnSabotage(player, systemType, amount);
        }
    }
    public static void Postfix(ShipStatus __instance)
    {
        Camouflage.CheckCamouflage();
    }
    public static void CheckAndOpenDoorsRange(ShipStatus __instance, int amount, int min, int max)
    {
        var Ids = new List<int>();
        for (var i = min; i <= max; i++)
        {
            Ids.Add(i);
        }
        CheckAndOpenDoors(__instance, amount, Ids.ToArray());
    }
    private static void CheckAndOpenDoors(ShipStatus __instance, int amount, params int[] DoorIds)
    {
        if (DoorIds.Contains(amount)) foreach (var id in DoorIds)
            {
                __instance.RpcRepairSystem(SystemTypes.Doors, id);
            }
    }
    private static bool CanSabotage(PlayerControl player, SystemTypes systemType)
    {
        //サボタージュ出来ないキラー役職はサボタージュ自体をキャンセル
        if (!player.Is(CustomRoleTypes.Impostor))
        {
            return false;
        }
        return true;
    }
    public static bool OnSabotage(PlayerControl player, SystemTypes systemType, byte amount)
    {
        // 停電サボタージュが鳴らされた場合は関係なし(ホスト名義で飛んでくるため誤爆注意)
        if (systemType == SystemTypes.Electrical && amount.HasBit(SwitchSystem.DamageSystem))
        {
            return true;
        }

        //Airshipの特定の停電を直せないならキャンセル
        if (systemType == SystemTypes.Electrical && Main.NormalOptions.MapId == 4)
        {
            var truePosition = player.GetTruePosition();
            if (Options.DisableAirshipViewingDeckLightsPanel.GetBool() && Vector2.Distance(truePosition, new(-12.93f, -11.28f)) <= 2f) return false;
            if (Options.DisableAirshipGapRoomLightsPanel.GetBool() && Vector2.Distance(truePosition, new(13.92f, 6.43f)) <= 2f) return false;
            if (Options.DisableAirshipCargoLightsPanel.GetBool() && Vector2.Distance(truePosition, new(30.56f, 2.12f)) <= 2f) return false;
        }
        return true;
    }
}
[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.CloseDoorsOfType))]
class CloseDoorsPatch
{
    public static bool Prefix(ShipStatus __instance)
    {
        return !(Options.DisableSabotage.GetBool() || Options.CurrentGameMode == CustomGameMode.SoloKombat);
    }
}
[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Start))]
class StartPatch
{
    public static void Postfix()
    {
        Logger.CurrentMethod();
        Logger.Info("-----------游戏开始-----------", "Phase");

        Utils.CountAlivePlayers(true);

        if (Options.AllowConsole.GetBool() || PlayerControl.LocalPlayer.IsDev())
        {
            if (!BepInEx.ConsoleManager.ConsoleActive && BepInEx.ConsoleManager.ConsoleEnabled)
                BepInEx.ConsoleManager.CreateConsole();
        }
        else
        {
            if (BepInEx.ConsoleManager.ConsoleActive && !DebugModeManager.AmDebugger)
            {
                BepInEx.ConsoleManager.DetachConsole();
                Logger.SendInGame("很抱歉，本房间禁止使用控制台，因此已将您的控制台关闭");
            }
        }
    }
}
[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.StartMeeting))]
class StartMeetingPatch
{
    public static void Prefix(ShipStatus __instance, PlayerControl reporter, GameData.PlayerInfo target)
    {
        MeetingStates.ReportTarget = target;
        MeetingStates.DeadBodies = UnityEngine.Object.FindObjectsOfType<DeadBody>();
    }
}
[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Begin))]
class BeginPatch
{
    public static void Postfix()
    {
        Logger.CurrentMethod();

        //ホストの役職初期設定はここで行うべき？
    }
}
[HarmonyPatch(typeof(GameManager), nameof(GameManager.CheckTaskCompletion))]
class CheckTaskCompletionPatch
{
    public static bool Prefix(ref bool __result)
    {
        if (Options.DisableTaskWin.GetBool() || Options.NoGameEnd.GetBool() || TaskState.InitialTotalTasks == 0 || Options.CurrentGameMode == CustomGameMode.SoloKombat)
        {
            __result = false;
            return false;
        }
        return true;
    }
}