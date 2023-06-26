using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;
using System;
using System.Collections.Generic;
using System.Linq;
using TOHE.Modules;
using TOHE.Roles.AddOns.Common;
using TOHE.Roles.AddOns.Crewmate;
using TOHE.Roles.AddOns.Impostor;
using TOHE.Roles.Core;
using static TOHE.Modules.CustomRoleSelector;
using static TOHE.Translator;

namespace TOHE;

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.CoStartGame))]
internal class ChangeRoleSettings
{
    public static void Postfix(AmongUsClient __instance)
    {
        try
        {
            //注:この時点では役職は設定されていません。
            Main.NormalOptions.roleOptions.SetRoleRate(RoleTypes.GuardianAngel, 0, 0);
            if (Options.DisableVanillaRoles.GetBool())
            {
                Main.NormalOptions.roleOptions.SetRoleRate(RoleTypes.Scientist, 0, 0);
                Main.NormalOptions.roleOptions.SetRoleRate(RoleTypes.Engineer, 0, 0);
                Main.NormalOptions.roleOptions.SetRoleRate(RoleTypes.Shapeshifter, 0, 0);
            }

            PlayerState.Clear();

            Main.OverrideWelcomeMsg = "";
            Main.AllPlayerKillCooldown = new();
            Main.AllPlayerSpeed = new();

            Main.LastEnteredVent = new();
            Main.LastEnteredVentLocation = new();

            Main.AfterMeetingDeathPlayers = new();
            Main.ResetCamPlayerList = new();
            Main.clientIdList = new();

            Main.CheckShapeshift = new();
            Main.ShapeshiftTarget = new();

            Main.ShieldPlayer = Options.ShieldPersonDiedFirst.GetBool() ? Main.FirstDied : byte.MaxValue;
            Main.FirstDied = byte.MaxValue;
            Main.MadmateNum = 0;

            ReportDeadBodyPatch.CanReport = new();

            Options.UsedButtonCount = 0;

            GameOptionsManager.Instance.currentNormalGameOptions.ConfirmImpostor = false;
            Main.RealOptionsData = new OptionBackupData(GameOptionsManager.Instance.CurrentGameOptions);

            Main.introDestroyed = false;

            RandomSpawn.CustomNetworkTransformPatch.NumOfTP = new();

            MeetingTimeManager.Init();
            Main.DefaultCrewmateVision = Main.RealOptionsData.GetFloat(FloatOptionNames.CrewLightMod);
            Main.DefaultImpostorVision = Main.RealOptionsData.GetFloat(FloatOptionNames.ImpostorLightMod);

            Main.LastNotifyNames = new();

            Main.PlayerColors = new();
            //名前の記録
            RPC.SyncAllPlayerNames();

            Camouflage.Init();
            //var invalidColor = Main.AllPlayerControls.Where(p => p.Data.DefaultOutfit.ColorId < 0 || Palette.PlayerColors.Length <= p.Data.DefaultOutfit.ColorId);
            //if (invalidColor.Count() != 0)
            //{
            //    var msg = GetString("Error.InvalidColor");
            //    Logger.SendInGame(msg);
            //    Utils.SendMessage(msg);
            //    Logger.Error(msg, "CoStartGame");
            //}

            foreach (var target in Main.AllPlayerControls)
            {
                foreach (var seer in Main.AllPlayerControls)
                {
                    var pair = (target.PlayerId, seer.PlayerId);
                    Main.LastNotifyNames[pair] = target.name;
                }
            }
            foreach (var pc in Main.AllPlayerControls)
            {
                var colorId = pc.Data.DefaultOutfit.ColorId;
                if (AmongUsClient.Instance.AmHost && Options.FormatNameMode.GetInt() == 1) pc.RpcSetName(Palette.GetColorName(colorId));
                PlayerState.Create(pc.PlayerId);
                //Main.AllPlayerNames[pc.PlayerId] = pc?.Data?.PlayerName;
                Main.PlayerColors[pc.PlayerId] = Palette.PlayerColors[colorId];
                Main.AllPlayerSpeed[pc.PlayerId] = Main.RealOptionsData.GetFloat(FloatOptionNames.PlayerSpeedMod); //移動速度をデフォルトの移動速度に変更
                ReportDeadBodyPatch.CanReport[pc.PlayerId] = true;
                ReportDeadBodyPatch.WaitReport[pc.PlayerId] = new();
                pc.cosmetics.nameText.text = pc.name;

                RandomSpawn.CustomNetworkTransformPatch.NumOfTP.Add(pc.PlayerId, 0);
                var outfit = pc.Data.DefaultOutfit;
                Camouflage.PlayerSkins[pc.PlayerId] = new GameData.PlayerOutfit().Set(outfit.PlayerName, outfit.ColorId, outfit.HatId, outfit.SkinId, outfit.VisorId, outfit.PetId);
                Main.clientIdList.Add(pc.GetClientId());
            }
            Main.VisibleTasksCount = true;
            if (__instance.AmHost)
            {
                RPC.SyncCustomSettingsRPC();
            }
            CustomRoleManager.Initialize();
            FallFromLadder.Reset();
            LastImpostor.Init();
            TargetArrow.Init();
            LocateArrow.Init();
            DoubleTrigger.Init();
            CustomWinnerHolder.Reset();
            NameNotifyManager.Reset();
            AntiBlackout.Reset();
            SoloKombatManager.Init();

            // 附加职业的初始化
            Watcher.Init();
            Workhorse.Init();

            IRandom.SetInstanceById(Options.RoleAssigningAlgorithm.GetValue());

            MeetingStates.MeetingCalled = false;
            MeetingStates.FirstMeeting = true;
            GameStates.AlreadyDied = false;
        }
        catch (Exception ex)
        {
            Utils.ErrorEnd("Change Role Setting Postfix");
            Logger.Fatal(ex.ToString(), "Change Role Setting Postfix");
        }
    }
}
[HarmonyPatch(typeof(RoleManager), nameof(RoleManager.SelectRoles))]
internal class SelectRolesPatch
{
    public static void Prefix()
    {
        if (!AmongUsClient.Instance.AmHost) return;

        try
        {
            //CustomRpcSenderとRpcSetRoleReplacerの初期化
            Dictionary<byte, CustomRpcSender> senders = new();
            foreach (var pc in Main.AllPlayerControls)
            {
                senders[pc.PlayerId] = new CustomRpcSender($"{pc.name}'s SetRole Sender", SendOption.Reliable, false)
                        .StartMessage(pc.GetClientId());
            }
            RpcSetRoleReplacer.StartReplace(senders);

            if (Options.EnableGM.GetBool())
            {
                PlayerControl.LocalPlayer.RpcSetCustomRole(CustomRoles.GM);
                PlayerControl.LocalPlayer.RpcSetRole(RoleTypes.Crewmate);
                PlayerControl.LocalPlayer.Data.IsDead = true;
                PlayerState.AllPlayerStates[PlayerControl.LocalPlayer.PlayerId].SetDead();
            }

            SelectCustomRoles();
            SelectAddonRoles();
            CalculateVanillaRoleCount();

            //指定原版特殊职业数量
            RoleTypes[] RoleTypesList = { RoleTypes.Scientist, RoleTypes.Engineer, RoleTypes.Shapeshifter };
            foreach (var roleTypes in RoleTypesList)
            {
                var roleOpt = Main.NormalOptions.roleOptions;
                int numRoleTypes = GetRoleTypesCount(roleTypes);
                roleOpt.SetRoleRate(roleTypes, numRoleTypes, numRoleTypes > 0 ? 100 : 0);
            }

            Dictionary<(byte, byte), RoleTypes> rolesMap = new();

            // 注册反职业
            foreach (var kv in RoleResult.Where(x => x.Value.IsDesyncRole()))
                AssignDesyncRole(kv.Value, kv.Key, senders, rolesMap, BaseRole: kv.Value.GetRoleInfo().BaseRoleType.Invoke());

            foreach (var cp in RoleResult.Where(x => x.Value == CustomRoles.Crewpostor))
                AssignDesyncRole(cp.Value, cp.Key, senders, rolesMap, BaseRole: RoleTypes.Crewmate, hostBaseRole: RoleTypes.Impostor);

            MakeDesyncSender(senders, rolesMap);

        }
        catch (Exception e)
        {
            Utils.ErrorEnd("Select Role Prefix");
            Logger.Fatal(e.Message, "Select Role Prefix");
        }
        //以下、バニラ側の役職割り当てが入る
    }

    public static void Postfix()
    {
        if (!AmongUsClient.Instance.AmHost) return;

        try
        {
            List<(PlayerControl, RoleTypes)> newList = new();
            foreach (var sd in RpcSetRoleReplacer.StoragedData)
            {
                var kp = RoleResult.Where(x => x.Key.PlayerId == sd.Item1.PlayerId).FirstOrDefault();
                if (kp.Value.IsDesyncRole() || kp.Value == CustomRoles.Crewpostor)
                {
                    Logger.Warn($"反向原版职业 => {sd.Item1.GetRealName()}: {sd.Item2}", "Override Role Select");
                    continue;
                }
                newList.Add((sd.Item1, kp.Value.GetRoleTypes()));
                if (sd.Item2 == kp.Value.GetRoleTypes())
                    Logger.Warn($"注册原版职业 => {sd.Item1.GetRealName()}: {sd.Item2}", "Override Role Select");
                else
                    Logger.Warn($"覆盖原版职业 => {sd.Item1.GetRealName()}: {sd.Item2} => {kp.Value.GetRoleTypes()}", "Override Role Select");
            }
            if (Options.EnableGM.GetBool()) newList.Add((PlayerControl.LocalPlayer, RoleTypes.Crewmate));
            RpcSetRoleReplacer.StoragedData = newList;

            RpcSetRoleReplacer.Release(); //保存していたSetRoleRpcを一気に書く
            RpcSetRoleReplacer.senders.Do(kvp => kvp.Value.SendMessage());

            // 不要なオブジェクトの削除
            RpcSetRoleReplacer.senders = null;
            RpcSetRoleReplacer.OverriddenSenderList = null;
            RpcSetRoleReplacer.StoragedData = null;

            //Utils.ApplySuffix();

            foreach (var pc in Main.AllPlayerControls)
            {
                pc.Data.IsDead = false; //プレイヤーの死を解除する
                if (PlayerState.AllPlayerStates[pc.PlayerId].MainRole != CustomRoles.NotAssigned) continue; //既にカスタム役職が割り当てられていればスキップ
                var role = CustomRoles.NotAssigned;
                switch (pc.Data.Role.Role)
                {
                    case RoleTypes.Crewmate:
                        role = CustomRoles.Crewmate;
                        break;
                    case RoleTypes.Impostor:
                        role = CustomRoles.Impostor;
                        break;
                    case RoleTypes.Scientist:
                        role = CustomRoles.Scientist;
                        break;
                    case RoleTypes.Engineer:
                        role = CustomRoles.Engineer;
                        break;
                    case RoleTypes.GuardianAngel:
                        role = CustomRoles.GuardianAngel;
                        break;
                    case RoleTypes.Shapeshifter:
                        role = CustomRoles.Shapeshifter;
                        break;
                    default:
                        Logger.SendInGame(string.Format(GetString("Error.InvalidRoleAssignment"), pc?.Data?.PlayerName));
                        break;
                }
                PlayerState.GetByPlayerId(pc.PlayerId).SetMainRole(role);
            }

            // 个人竞技模式用
            if (Options.CurrentGameMode == CustomGameMode.SoloKombat)
            {
                foreach (var pair in PlayerState.AllPlayerStates)
                    ExtendedPlayerControl.RpcSetCustomRole(pair.Key, pair.Value.MainRole);
                goto EndOfSelectRolePatch;
            }

            var rd = IRandom.Instance;

            foreach (var kv in RoleResult)
            {
                if (kv.Value.IsDesyncRole()) continue;
                AssignCustomRole(kv.Value, kv.Key);
            }

            if (CustomRoles.Lovers.IsEnable() && CustomRoles.FFF.IsEnable()) AssignLoversRoles();
            else if (CustomRoles.Lovers.IsEnable() && rd.Next(0, 100) < Options.GetRoleChance(CustomRoles.Lovers)) AssignLoversRoles();
            if (CustomRoles.Madmate.IsEnable() && Options.MadmateSpawnMode.GetInt() == 0) AssignMadmateRoles();
            AddOnsAssignData.AssignAddOnsFromList();

            foreach (var pair in PlayerState.AllPlayerStates)
            {
                ExtendedPlayerControl.RpcSetCustomRole(pair.Key, pair.Value.MainRole);

                foreach (var subRole in pair.Value.SubRoles)
                    ExtendedPlayerControl.RpcSetCustomRole(pair.Key, subRole);
            }

        EndOfSelectRolePatch:

            CustomRoleManager.CreateInstance();
            foreach (var pc in Main.AllPlayerControls)
            {
                HudManager.Instance.SetHudActive(true);
                pc.ResetKillCooldown();
            }

            RoleTypes[] RoleTypesList = { RoleTypes.Scientist, RoleTypes.Engineer, RoleTypes.Shapeshifter };
            foreach (var roleTypes in RoleTypesList)
            {
                var roleOpt = Main.NormalOptions.roleOptions;
                roleOpt.SetRoleRate(roleTypes, 0, 0);
            }

            switch (Options.CurrentGameMode)
            {
                case CustomGameMode.Standard:
                    GameEndChecker.SetPredicateToNormal();
                    break;
                case CustomGameMode.SoloKombat:
                    GameEndChecker.SetPredicateToSoloKombat();
                    break;
            }

            GameOptionsSender.AllSenders.Clear();
            foreach (var pc in Main.AllPlayerControls)
            {
                GameOptionsSender.AllSenders.Add(
                    new PlayerGameOptionsSender(pc)
                );
            }

            /*
            //インポスターのゴーストロールがクルーになるバグ対策
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (pc.Data.Role.IsImpostor || Main.ResetCamPlayerList.Contains(pc.PlayerId))
                {
                    pc.Data.Role.DefaultGhostRole = RoleTypes.ImpostorGhost;
                }
            }
            */
            Utils.CountAlivePlayers(true);
            Utils.SyncAllSettings();
            SetColorPatch.IsAntiGlitchDisabled = false;
        }
        catch (Exception ex)
        {
            Utils.ErrorEnd("Select Role Postfix");
            Logger.Fatal(ex.ToString(), "Select Role Prefix");
        }
    }
    private static void AssignDesyncRole(CustomRoles role, PlayerControl player, Dictionary<byte, CustomRpcSender> senders, Dictionary<(byte, byte), RoleTypes> rolesMap, RoleTypes BaseRole, RoleTypes hostBaseRole = RoleTypes.Crewmate)
    {
        if (!role.Exist(true)) return;

        var hostId = PlayerControl.LocalPlayer.PlayerId;

        PlayerState.GetByPlayerId(player.PlayerId).SetMainRole(role);

        var selfRole = player.PlayerId == hostId ? hostBaseRole : BaseRole;
        var othersRole = player.PlayerId == hostId ? RoleTypes.Crewmate : RoleTypes.Scientist;

        //Desync役職視点
        foreach (var target in Main.AllPlayerControls)
            rolesMap[(player.PlayerId, target.PlayerId)] = player.PlayerId != target.PlayerId ? othersRole : selfRole;

        //他者視点
        foreach (var seer in Main.AllPlayerControls.Where(x => player.PlayerId != x.PlayerId))
            rolesMap[(seer.PlayerId, player.PlayerId)] = othersRole;

        RpcSetRoleReplacer.OverriddenSenderList.Add(senders[player.PlayerId]);
        //ホスト視点はロール決定
        player.SetRole(othersRole);
        player.Data.IsDead = true;

        Logger.Info($"注册模组职业：{player?.Data?.PlayerName} => {role}", "AssignCustomSubRoles");
    }
    public static void MakeDesyncSender(Dictionary<byte, CustomRpcSender> senders, Dictionary<(byte, byte), RoleTypes> rolesMap)
    {
        foreach (var seer in Main.AllPlayerControls)
        {
            var sender = senders[seer.PlayerId];
            foreach (var target in Main.AllPlayerControls)
            {
                if (rolesMap.TryGetValue((seer.PlayerId, target.PlayerId), out var role))
                {
                    sender.RpcSetRole(seer, role, target.GetClientId());
                }
            }
        }
    }

    private static void AssignCustomRole(CustomRoles role, PlayerControl player)
    {
        if (player == null) return;
        SetColorPatch.IsAntiGlitchDisabled = true;

        PlayerState.GetByPlayerId(player.PlayerId).SetMainRole(role);
        Logger.Info($"注册模组职业：{player?.Data?.PlayerName} => {role}", "AssignCustomRoles");

        SetColorPatch.IsAntiGlitchDisabled = false;
    }
    private static void AssignLoversRoles(int RawCount = -1)
    {
        //Loversを初期化
        Main.LoversPlayers.Clear();
        Main.isLoversDead = false;
        var allPlayers = new List<PlayerControl>();
        foreach (var pc in Main.AllPlayerControls)
        {
            if (pc.Is(CustomRoles.GM) || (PlayerState.GetByPlayerId(pc.PlayerId).SubRoles.Count >= Options.AddonsNumLimit.GetInt())
                || pc.Is(CustomRoles.Needy) || pc.Is(CustomRoles.Ntr) || pc.Is(CustomRoles.God) || pc.Is(CustomRoles.FFF)) continue;
            allPlayers.Add(pc);
        }
        var loversRole = CustomRoles.Lovers;
        var rd = IRandom.Instance;
        var count = Math.Clamp(RawCount, 0, allPlayers.Count);
        if (RawCount == -1) count = Math.Clamp(loversRole.GetCount(), 0, allPlayers.Count);
        if (count <= 0) return;
        for (var i = 0; i < count; i++)
        {
            var player = allPlayers[rd.Next(0, allPlayers.Count)];
            Main.LoversPlayers.Add(player);
            allPlayers.Remove(player);
            PlayerState.GetByPlayerId(player.PlayerId).SetSubRole(loversRole);
            Logger.Info($"注册附加职业：{player?.Data?.PlayerName}（{player.GetCustomRole()}）=> {loversRole}", "AssignCustomSubRoles");
        }
        RPC.SyncLoversPlayers();
    }
    private static void AssignMadmateRoles()
    {
        var allPlayers = Main.AllPlayerControls.Where(x => x.CanBeMadmate()).ToList();
        var count = Math.Clamp(CustomRoles.Madmate.GetCount(), 0, allPlayers.Count);
        if (count <= 0) return;
        for (var i = 0; i < count; i++)
        {
            var player = allPlayers[IRandom.Instance.Next(0, allPlayers.Count)];
            allPlayers.Remove(player);
            PlayerState.GetByPlayerId(player.PlayerId).SetSubRole(CustomRoles.Madmate);
            Logger.Info($"注册附加职业：{player?.Data?.PlayerName}（{player.GetCustomRole()}）=> {CustomRoles.Madmate}", "AssignCustomSubRoles");
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSetRole))]
    private class RpcSetRoleReplacer
    {
        public static bool doReplace = false;
        public static Dictionary<byte, CustomRpcSender> senders;
        public static List<(PlayerControl, RoleTypes)> StoragedData = new();
        // 役職Desyncなど別の処理でSetRoleRpcを書き込み済みなため、追加の書き込みが不要なSenderのリスト
        public static List<CustomRpcSender> OverriddenSenderList;
        public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] RoleTypes roleType)
        {
            if (doReplace && senders != null)
            {
                StoragedData.Add((__instance, roleType));
                return false;
            }
            else return true;
        }
        public static void Release()
        {
            foreach (var sender in senders)
            {
                if (OverriddenSenderList.Contains(sender.Value)) continue;
                if (sender.Value.CurrentState != CustomRpcSender.State.InRootMessage)
                    throw new InvalidOperationException("A CustomRpcSender had Invalid State.");

                foreach (var pair in StoragedData)
                {
                    pair.Item1.SetRole(pair.Item2);
                    sender.Value.AutoStartRpc(pair.Item1.NetId, (byte)RpcCalls.SetRole, Utils.GetPlayerById(sender.Key).GetClientId())
                        .Write((ushort)pair.Item2)
                        .EndRpc();
                }
                sender.Value.EndMessage();
            }
            doReplace = false;
        }
        public static void StartReplace(Dictionary<byte, CustomRpcSender> senders)
        {
            RpcSetRoleReplacer.senders = senders;
            StoragedData = new();
            OverriddenSenderList = new();
            doReplace = true;
        }
    }
}