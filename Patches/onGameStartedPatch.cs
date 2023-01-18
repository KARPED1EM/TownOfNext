using System;
using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;
using Sentry.Internal.Http;
using TownOfHost.Modules;
using static TownOfHost.Translator;

namespace TownOfHost
{
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.CoStartGame))]
    class ChangeRoleSettings
    {
        public static void Postfix(AmongUsClient __instance)
        {
            //注：暂未设置标题
            Main.PlayerStates = new();

            Main.AllPlayerKillCooldown = new Dictionary<byte, float>();
            Main.AllPlayerSpeed = new Dictionary<byte, float>();
            Main.BitPlayers = new Dictionary<byte, (byte, float)>();
            Main.WarlockTimer = new Dictionary<byte, float>();
            Main.isDoused = new Dictionary<(byte, byte), bool>();
            Main.ArsonistTimer = new Dictionary<byte, (PlayerControl, float)>();
            Main.CursedPlayers = new Dictionary<byte, PlayerControl>();
            Main.isCurseAndKill = new Dictionary<byte, bool>();
            Main.SKMadmateNowCount = 0;
            Main.isCursed = false;
            Main.PuppeteerList = new Dictionary<byte, byte>();
            Main.HackerUsedCount = new Dictionary<byte, int>();

            Main.LastEnteredVent = new Dictionary<byte, Vent>();
            Main.LastEnteredVentLocation = new Dictionary<byte, UnityEngine.Vector2>();
            
            Main.AfterMeetingDeathPlayers = new();
            Main.ResetCamPlayerList = new();
            Main.clientIdList = new();

            Main.CheckShapeshift = new();
            Main.ShapeshiftTarget = new();
            Main.SpeedBoostTarget = new Dictionary<byte, byte>();
            Main.MayorUsedButtonCount = new Dictionary<byte, int>();
            Main.targetArrows = new();

            ReportDeadBodyPatch.CanReport = new();

            Options.UsedButtonCount = 0;
            Main.RealOptionsData = new OptionBackupData(GameOptionsManager.Instance.CurrentGameOptions);

            Main.introDestroyed = false;

            RandomSpawn.CustomNetworkTransformPatch.NumOfTP = new();

            Main.DiscussionTime = Main.RealOptionsData.GetInt(Int32OptionNames.DiscussionTime);
            Main.VotingTime = Main.RealOptionsData.GetInt(Int32OptionNames.VotingTime);
            Main.DefaultCrewmateVision = Main.RealOptionsData.GetFloat(FloatOptionNames.CrewLightMod);
            Main.DefaultImpostorVision = Main.RealOptionsData.GetFloat(FloatOptionNames.ImpostorLightMod);

            NameColorManager.Instance.RpcReset();
            Main.LastNotifyNames = new();

            Main.currentDousingTarget = 255;
            Main.PlayerColors = new();
            //名字记录
            Main.AllPlayerNames = new();

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
                if (AmongUsClient.Instance.AmHost && Options.ColorNameMode.GetBool()) pc.RpcSetName(Palette.GetColorName(pc.Data.DefaultOutfit.ColorId));
                Main.PlayerStates[pc.PlayerId] = new(pc.PlayerId);
                Main.AllPlayerNames[pc.PlayerId] = pc?.Data?.PlayerName;

                Main.PlayerColors[pc.PlayerId] = Palette.PlayerColors[pc.Data.DefaultOutfit.ColorId];
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
                Main.RefixCooldownDelay = 0;
                if (Options.CurrentGameMode == CustomGameMode.HideAndSeek)
                {
                    Options.HideAndSeekKillDelayTimer = Options.KillDelay.GetFloat();
                }
                if (Options.IsStandardHAS)
                {
                    Options.HideAndSeekKillDelayTimer = Options.StandardHASWaitingTime.GetFloat();
                }
            }
            FallFromLadder.Reset();
            BountyHunter.Init();
            SerialKiller.Init();
            FireWorks.Init();
            Sniper.Init();
            TimeThief.Init();
            Mare.Init();
            Witch.Init();
            SabotageMaster.Init();
            Egoist.Init();
            Executioner.Init();
            Jackal.Init();
            Sheriff.Init();
            EvilTracker.Init();
            LastImpostor.Init();
            CustomWinnerHolder.Reset();
            AntiBlackout.Reset();
            IRandom.SetInstanceById(Options.RoleAssigningAlgorithm.GetValue());

            MeetingStates.MeetingCalled = false;
            MeetingStates.FirstMeeting = true;
            GameStates.AlreadyDied = false;
        }
    }
    [HarmonyPatch(typeof(RoleManager), nameof(RoleManager.SelectRoles))]
    class SelectRolesPatch
    {
        public static void Prefix()
        {
            if (!AmongUsClient.Instance.AmHost) return;

            Main.assignedNK = 0;
            Main.assignedNNK = 0;

            Random rd = new();
            Main.needOfNK = rd.Next(0, Options.MaxNK.GetInt() + 1);
            if (Main.needOfNK > Options.MaxNK.GetInt()) Main.needOfNK = Options.MaxNK.GetInt();
            Main.needOfNNK = rd.Next(0, Options.MaxNNK.GetInt() + 1);
            if (Main.needOfNNK > Options.MaxNNK.GetInt()) Main.needOfNNK = Options.MaxNNK.GetInt();

            //初始化 CustomRpcSender 和 RpcSetRoleReplacer
            Dictionary<byte, CustomRpcSender> senders = new();
            foreach (var pc in Main.AllPlayerControls)
            {
                senders[pc.PlayerId] = new CustomRpcSender($"{pc.name}'s SetRole Sender", SendOption.Reliable, false)
                    .StartMessage(pc.GetClientId());
            }
            RpcSetRoleReplacer.StartReplace(senders);

            //抽取观察者的阵营
            Options.SetWatcherTeam(Options.EvilWatcherChance.GetFloat());

            if (Options.CurrentGameMode != CustomGameMode.HideAndSeek)
            {
                //指定各角色人数
                var roleOpt = Main.NormalOptions.roleOptions;
                int ScientistNum = roleOpt.GetNumPerGame(RoleTypes.Scientist);
                int AdditionalScientistNum = CustomRoles.Doctor.GetCount();
                roleOpt.SetRoleRate(RoleTypes.Scientist, ScientistNum + AdditionalScientistNum, AdditionalScientistNum > 0 ? 100 : roleOpt.GetChancePerGame(RoleTypes.Scientist));

                int EngineerNum = roleOpt.GetNumPerGame(RoleTypes.Engineer);

                int AdditionalEngineerNum = CustomRoles.Madmate.GetCount() + CustomRoles.Terrorist.GetCount() + CustomRoles.Paranoia.GetCount() + CustomRoles.Plumber.GetCount();// - EngineerNum;

                if (Options.MayorHasPortableButton.GetBool())
                    AdditionalEngineerNum += CustomRoles.Mayor.GetCount();

                if (Options.MadSnitchCanVent.GetBool())
                    AdditionalEngineerNum += CustomRoles.MadSnitch.GetCount();

                roleOpt.SetRoleRate(RoleTypes.Engineer, EngineerNum + AdditionalEngineerNum, AdditionalEngineerNum > 0 ? 100 : roleOpt.GetChancePerGame(RoleTypes.Engineer));

                int ShapeshifterNum = roleOpt.GetNumPerGame(RoleTypes.Shapeshifter);
                int AdditionalShapeshifterNum = CustomRoles.SerialKiller.GetCount() + CustomRoles.BountyHunter.GetCount() + CustomRoles.Warlock.GetCount() + CustomRoles.Miner.GetCount() + CustomRoles.FireWorks.GetCount() + CustomRoles.Sniper.GetCount() + CustomRoles.EvilTracker.GetCount();//- ShapeshifterNum;
                if (Main.RealOptionsData.GetInt(Int32OptionNames.NumImpostors) > 1)
                    AdditionalShapeshifterNum += CustomRoles.Egoist.GetCount();
                roleOpt.SetRoleRate(RoleTypes.Shapeshifter, ShapeshifterNum + AdditionalShapeshifterNum, AdditionalShapeshifterNum > 0 ? 100 : roleOpt.GetChancePerGame(RoleTypes.Shapeshifter));


                List<PlayerControl> AllPlayers = new();
                foreach (var pc in Main.AllPlayerControls)
                {
                    AllPlayers.Add(pc);
                }

                if (Options.EnableGM.GetBool())
                {
                    AllPlayers.RemoveAll(x => x == PlayerControl.LocalPlayer);
                    PlayerControl.LocalPlayer.RpcSetCustomRole(CustomRoles.GM);
                    PlayerControl.LocalPlayer.RpcSetRole(RoleTypes.Crewmate);
                    PlayerControl.LocalPlayer.Data.IsDead = true;
                }
                Dictionary<(byte, byte), RoleTypes> rolesMap = new();
                AssignDesyncRole(CustomRoles.Sheriff, AllPlayers, senders, rolesMap, BaseRole: RoleTypes.Impostor);
                AssignDesyncRole(CustomRoles.Arsonist, AllPlayers, senders, rolesMap, BaseRole: RoleTypes.Impostor);
                AssignDesyncRole(CustomRoles.Jackal, AllPlayers, senders, rolesMap, BaseRole: RoleTypes.Impostor);
                MakeDesyncSender(senders, rolesMap);
            }
            //以下是原本游戏的职位分配
        }
        public static void Postfix()
        {
            if (!AmongUsClient.Instance.AmHost) return;
            RpcSetRoleReplacer.Release(); //保存していたSetRoleRpcを一気に書く
            RpcSetRoleReplacer.senders.Do(kvp => kvp.Value.SendMessage());

            //删除不需要的对象
            RpcSetRoleReplacer.senders = null;
            RpcSetRoleReplacer.OverriddenSenderList = null;
            RpcSetRoleReplacer.StoragedData = null;

            //Utils.ApplySuffix();

            var rand = IRandom.Instance;

            List<PlayerControl> Crewmates = new();
            List<PlayerControl> Impostors = new();
            List<PlayerControl> Scientists = new();
            List<PlayerControl> Engineers = new();
            List<PlayerControl> GuardianAngels = new();
            List<PlayerControl> Shapeshifters = new();

            foreach (var pc in Main.AllPlayerControls)
            {
                pc.Data.IsDead = false; //プレイヤーの死を解除する
                if (Main.PlayerStates[pc.PlayerId].MainRole != CustomRoles.NotAssigned) continue; //如果已分配自定义角色则跳过
                var role = CustomRoles.NotAssigned;
                switch (pc.Data.Role.Role)
                {
                    case RoleTypes.Crewmate:
                        Crewmates.Add(pc);
                        role = CustomRoles.Crewmate;
                        break;
                    case RoleTypes.Impostor:
                        Impostors.Add(pc);
                        role = CustomRoles.Impostor;
                        break;
                    case RoleTypes.Scientist:
                        Scientists.Add(pc);
                        role = CustomRoles.Scientist;
                        break;
                    case RoleTypes.Engineer:
                        Engineers.Add(pc);
                        role = CustomRoles.Engineer;
                        break;
                    case RoleTypes.GuardianAngel:
                        GuardianAngels.Add(pc);
                        role = CustomRoles.GuardianAngel;
                        break;
                    case RoleTypes.Shapeshifter:
                        Shapeshifters.Add(pc);
                        role = CustomRoles.Shapeshifter;
                        break;
                    default:
                        Logger.SendInGame(string.Format(GetString("Error.InvalidRoleAssignment"), pc?.Data?.PlayerName));
                        break;
                }
                Main.PlayerStates[pc.PlayerId].MainRole = role;
            }

            if (Options.CurrentGameMode == CustomGameMode.HideAndSeek)
            {
                SetColorPatch.IsAntiGlitchDisabled = true;
                foreach (var pc in Main.AllPlayerControls)
                {
                    if (pc.Is(RoleType.Impostor))
                        pc.RpcSetColor(0);
                    else if (pc.Is(RoleType.Crewmate))
                        pc.RpcSetColor(1);
                }

                //后期设定流程
                AssignCustomRolesFromList(-1, CustomRoles.HASFox, Crewmates);
                AssignCustomRolesFromList(-1, CustomRoles.HASTroll, Crewmates);
                foreach (var pair in Main.PlayerStates)
                {
                    //通过 RPC 同步
                    ExtendedPlayerControl.RpcSetCustomRole(pair.Key, pair.Value.MainRole);
                }
                //颜色设置过程
                SetColorPatch.IsAntiGlitchDisabled = true;

                GameEndChecker.SetPredicateToHideAndSeek();
            }
            else
            {


                
                for (int i = 0; i <= 38; i++)
                {
                    Main.funList.Add(i);
                }
                
                Random rd = new();
                int index = 0;
                int temp;
                for (int i = 0; i < Main.funList.Count; i++)
                {
                    index = rd.Next(0, Main.funList.Count - 1);
                    if (index != i)
                    {
                        temp = Main.funList[i];
                        Main.funList[i] = Main.funList[index];
                        Main.funList[index] = temp;
                    }
                }

                if(Main.funList.Remove(38)) Main.funList.Insert(0, 38);

                int retryTimes = 0;
                Retry: retryTimes++;
                foreach (int i in Main.funList)
                {
                    switch (i)
                    {
                        case 0: AssignCustomRolesFromList(i, CustomRoles.FireWorks, Shapeshifters); break;
                        case 1: AssignCustomRolesFromList(i, CustomRoles.Sniper, Shapeshifters); break;
                        case 2: AssignCustomRolesFromList(i, CustomRoles.Jester, Crewmates); break;
                        case 3: AssignCustomRolesFromList(i, CustomRoles.Madmate, Engineers); break;
                        case 4: AssignCustomRolesFromList(i, CustomRoles.Bait, Crewmates); break;
                        case 5: AssignCustomRolesFromList(i, CustomRoles.MadGuardian, Crewmates); break;
                        case 6: AssignCustomRolesFromList(i, CustomRoles.MadSnitch, Options.MadSnitchCanVent.GetBool() ? Engineers : Crewmates); break;
                        case 7: AssignCustomRolesFromList(i, CustomRoles.Mayor, Options.MayorHasPortableButton.GetBool() ? Engineers : Crewmates); break;
                        case 8: AssignCustomRolesFromList(i, CustomRoles.Opportunist, Crewmates); break;
                        case 9: AssignCustomRolesFromList(i, CustomRoles.Snitch, Crewmates); break;
                        case 10: AssignCustomRolesFromList(i, CustomRoles.SabotageMaster, Crewmates); break;
                        case 11: AssignCustomRolesFromList(i, CustomRoles.Mafia, Impostors); break;
                        case 12: AssignCustomRolesFromList(i, CustomRoles.Terrorist, Engineers); break;
                        case 13: AssignCustomRolesFromList(i, CustomRoles.Executioner, Crewmates); break;
                        case 14: AssignCustomRolesFromList(i, CustomRoles.Vampire, Impostors); break;
                        case 15: AssignCustomRolesFromList(i, CustomRoles.BountyHunter, Shapeshifters); break;
                        case 16: AssignCustomRolesFromList(i, CustomRoles.Witch, Impostors); break;
                        case 17: AssignCustomRolesFromList(i, CustomRoles.Warlock, Shapeshifters); break;
                        case 18: AssignCustomRolesFromList(i, CustomRoles.SerialKiller, Shapeshifters); break;
                        case 19: AssignCustomRolesFromList(i, CustomRoles.Lighter, Crewmates); break;
                        case 20: AssignCustomRolesFromList(i, CustomRoles.SpeedBooster, Crewmates); break;
                        case 21: AssignCustomRolesFromList(i, CustomRoles.Trapper, Crewmates); break;
                        case 22: AssignCustomRolesFromList(i, CustomRoles.Dictator, Crewmates); break;
                        case 23: AssignCustomRolesFromList(i, CustomRoles.SchrodingerCat, Crewmates); break;
                        case 24: if (Options.IsEvilWatcher) AssignCustomRolesFromList(i, CustomRoles.Watcher, Impostors); else AssignCustomRolesFromList(i, CustomRoles.Watcher, Crewmates); break;
                        case 25: if (Main.RealOptionsData.GetInt(Int32OptionNames.NumImpostors) > 1) AssignCustomRolesFromList(i, CustomRoles.Egoist, Shapeshifters); break;
                        case 26: AssignCustomRolesFromList(i, CustomRoles.Mare, Impostors); break;
                        case 27: AssignCustomRolesFromList(i, CustomRoles.Doctor, Scientists); break;
                        case 28: AssignCustomRolesFromList(i, CustomRoles.Puppeteer, Impostors); break;
                        case 29: AssignCustomRolesFromList(i, CustomRoles.TimeThief, Impostors); break;
                        case 30: AssignCustomRolesFromList(i, CustomRoles.EvilTracker, Shapeshifters); break;
                        case 31: AssignCustomRolesFromList(i, CustomRoles.Seer, Crewmates); break;
                        case 32: AssignCustomRolesFromList(i, CustomRoles.Paranoia, Engineers); break;
                        case 33: AssignCustomRolesFromList(i, CustomRoles.Miner, Shapeshifters); break;
                        case 34: AssignCustomRolesFromList(i, CustomRoles.Psychic, Crewmates); break;
                        case 35: AssignCustomRolesFromList(i, CustomRoles.Plumber, Engineers); break;
                        case 36: AssignCustomRolesFromList(i, CustomRoles.Needy, Crewmates); break;
                        case 37: AssignCustomRolesFromList(i, CustomRoles.SuperStar, Crewmates); break;
                        case 38: AssignCustomRolesFromList(i, CustomRoles.Hacker, Impostors); break;
                    }
                }

                if (retryTimes < 3)
                {
                    foreach (PlayerControl pc in Impostors)
                    {
                        Logger.Info("存在未注册的内鬼职业，尝试改为变形重新分配", "Assign Roles");
                        Main.PlayerStates[pc.PlayerId].MainRole = CustomRoles.Shapeshifter;
                        Impostors.Remove(pc);
                        Shapeshifters.Add(pc);
                        goto Retry;
                    }
                    foreach (PlayerControl pc in Shapeshifters)
                    {
                        Logger.Info("存在未注册的变形职业，尝试改为内鬼重新分配", "Assign Roles");
                        Main.PlayerStates[pc.PlayerId].MainRole = CustomRoles.Impostor;
                        Shapeshifters.Remove(pc);
                        Impostors.Add(pc);
                        goto Retry;
                    }
                    foreach (PlayerControl pc in Engineers)
                    {
                        Logger.Info("存在未注册的工程师职业，尝试改为船员重新分配", "Assign Roles");
                        Main.PlayerStates[pc.PlayerId].MainRole = CustomRoles.Crewmate;
                        Engineers.Remove(pc);
                        Crewmates.Add(pc);
                        goto Retry;
                    }
                    foreach (PlayerControl pc in Crewmates)
                    {
                        Logger.Info("存在未注册的船员职业，尝试改为科学家重新分配", "Assign Roles");
                        Main.PlayerStates[pc.PlayerId].MainRole = CustomRoles.Scientist;
                        Crewmates.Remove(pc);
                        Scientists.Add(pc);
                        goto Retry;
                    }
                    foreach (PlayerControl pc in Scientists)
                    {
                        Logger.Info("存在未注册的科学家职业，尝试改为工程师重新分配", "Assign Roles");
                        Main.PlayerStates[pc.PlayerId].MainRole = CustomRoles.Engineer;
                        Scientists.Remove(pc);
                        Engineers.Add(pc);
                        goto Retry;
                    }
                }
                else
                {
                    Logger.Error("存在未注册的职业，五次分配均无效", "Assign Roles");
                }
                


                //通过 RPC 同步
                foreach (var pc in Main.AllPlayerControls)
                {
                    if (pc.Is(CustomRoles.Watcher))
                        Main.PlayerStates[pc.PlayerId].MainRole = Options.IsEvilWatcher ? CustomRoles.EvilWatcher : CustomRoles.NiceWatcher;
                }
                foreach (var pair in Main.PlayerStates)
                {
                    ExtendedPlayerControl.RpcSetCustomRole(pair.Key, pair.Value.MainRole);


                    foreach (var subRole in pair.Value.SubRoles)
                        ExtendedPlayerControl.RpcSetCustomRole(pair.Key, subRole);
                }

                HudManager.Instance.SetHudActive(true);
                foreach (var pc in Main.AllPlayerControls)
                {
                    if (pc.Data.Role.Role == RoleTypes.Shapeshifter) Main.CheckShapeshift.Add(pc.PlayerId, false);
                    switch (pc.GetCustomRole())
                    {
                        case CustomRoles.BountyHunter:
                            BountyHunter.Add(pc.PlayerId);
                            break;
                        case CustomRoles.SerialKiller:
                            SerialKiller.Add(pc.PlayerId);
                            break;
                        case CustomRoles.Witch:
                            Witch.Add(pc.PlayerId);
                            break;
                        case CustomRoles.Warlock:
                            Main.CursedPlayers.Add(pc.PlayerId, null);
                            Main.isCurseAndKill.Add(pc.PlayerId, false);
                            break;
                        case CustomRoles.FireWorks:
                            FireWorks.Add(pc.PlayerId);
                            break;
                        case CustomRoles.TimeThief:
                            TimeThief.Add(pc.PlayerId);
                            break;
                        case CustomRoles.Sniper:
                            Sniper.Add(pc.PlayerId);
                            break;
                        case CustomRoles.Mare:
                            Mare.Add(pc.PlayerId);
                            break;

                        case CustomRoles.Arsonist:
                            foreach (var ar in Main.AllPlayerControls)
                                Main.isDoused.Add((pc.PlayerId, ar.PlayerId), false);
                            break;
                        case CustomRoles.Executioner:
                            Executioner.Add(pc.PlayerId);
                            break;
                        case CustomRoles.Egoist:
                            Egoist.Add(pc.PlayerId);
                            break;
                        case CustomRoles.Jackal:
                            Jackal.Add(pc.PlayerId);
                            break;

                        case CustomRoles.Sheriff:
                            Sheriff.Add(pc.PlayerId);
                            break;
                        case CustomRoles.Mayor:
                            Main.MayorUsedButtonCount[pc.PlayerId] = 0;
                            break;
                        case CustomRoles.SabotageMaster:
                            SabotageMaster.Add(pc.PlayerId);
                            break;
                        case CustomRoles.EvilTracker:
                            EvilTracker.Add(pc.PlayerId);
                            break;
                        case CustomRoles.Hacker:
                            Main.HackerUsedCount[pc.PlayerId] = 0;
                            break;
                    }
                    pc.ResetKillCooldown();

                    //对于那些在普通模式下玩捉迷藏的人
                    if (Options.IsStandardHAS)
                    {
                        foreach (var seer in Main.AllPlayerControls)
                        {
                            if (seer == pc) continue;
                            if (pc.GetCustomRole().IsImpostor() || pc.IsNeutralKiller()) //变更目标为内鬼阵营或带刀中立
                                NameColorManager.Instance.RpcAdd(seer.PlayerId, pc.PlayerId, $"{pc.GetRoleColorCode()}");
                        }
                    }
                }

                //返回该职位的人数
                var roleOpt = Main.NormalOptions.roleOptions;
                int ScientistNum = roleOpt.GetNumPerGame(RoleTypes.Scientist);
                ScientistNum -= CustomRoles.Doctor.GetCount();
                roleOpt.SetRoleRate(RoleTypes.Scientist, ScientistNum, roleOpt.GetChancePerGame(RoleTypes.Scientist));

                int EngineerNum = roleOpt.GetNumPerGame(RoleTypes.Engineer);

                EngineerNum -= CustomRoles.Madmate.GetCount() + CustomRoles.Terrorist.GetCount() + CustomRoles.Paranoia.GetCount() + CustomRoles.Plumber.GetCount();

                if (Options.MayorHasPortableButton.GetBool())
                    EngineerNum -= CustomRoles.Mayor.GetCount();

                if (Options.MadSnitchCanVent.GetBool())
                    EngineerNum -= CustomRoles.MadSnitch.GetCount();

                roleOpt.SetRoleRate(RoleTypes.Engineer, EngineerNum, roleOpt.GetChancePerGame(RoleTypes.Engineer));

                int ShapeshifterNum = roleOpt.GetNumPerGame(RoleTypes.Shapeshifter);
                ShapeshifterNum -= CustomRoles.SerialKiller.GetCount() + CustomRoles.BountyHunter.GetCount() + CustomRoles.Warlock.GetCount() + CustomRoles.Miner.GetCount() + CustomRoles.FireWorks.GetCount() + CustomRoles.Sniper.GetCount() + CustomRoles.EvilTracker.GetCount();
                if (Main.RealOptionsData.GetInt(Int32OptionNames.NumImpostors) > 1)
                    ShapeshifterNum -= CustomRoles.Egoist.GetCount();
                roleOpt.SetRoleRate(RoleTypes.Shapeshifter, ShapeshifterNum, roleOpt.GetChancePerGame(RoleTypes.Shapeshifter));
                GameEndChecker.SetPredicateToNormal();

                GameOptionsSender.AllSenders.Clear();
                foreach (var pc in Main.AllPlayerControls)
                {
                    GameOptionsSender.AllSenders.Add(
                        new PlayerGameOptionsSender(pc)
                    );
                }
            }

            // ResetCamが必要なプレイヤーのリストにクラス化が済んでいない役職のプレイヤーを追加
            Main.ResetCamPlayerList.AddRange(Main.AllPlayerControls.Where(p => p.GetCustomRole() is CustomRoles.Arsonist).Select(p => p.PlayerId));
            /*
            //インポスターのゴーストロールがクルーメイトになるバグ対策
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (pc.Data.Role.IsImpostor || Main.ResetCamPlayerList.Contains(pc.PlayerId))
                {
                    pc.Data.Role.DefaultGhostRole = RoleTypes.ImpostorGhost;
                }
            }
            */
            Utils.CountAliveImpostors();
            Utils.SyncAllSettings();
            SetColorPatch.IsAntiGlitchDisabled = false;
        }
        private static void AssignDesyncRole(CustomRoles role, List<PlayerControl> AllPlayers, Dictionary<byte, CustomRpcSender> senders, Dictionary<(byte, byte), RoleTypes> rolesMap, RoleTypes BaseRole, RoleTypes hostBaseRole = RoleTypes.Crewmate)
        {
            if (!role.IsEnable()) return;

            if (CustomRolesHelper.IsNK(role) && Main.assignedNK >= Main.needOfNK) return;
            if (CustomRolesHelper.IsNNK(role) && Main.assignedNNK >= Main.needOfNNK) return;
            if (CustomRolesHelper.IsNK(role)) Main.assignedNK++;
            if (CustomRolesHelper.IsNNK(role)) Main.assignedNNK++;

            var hostId = PlayerControl.LocalPlayer.PlayerId;
            var rand = IRandom.Instance;

            for (var i = 0; i < role.GetCount(); i++)
            {
                if (AllPlayers.Count <= 0) break;
                var player = AllPlayers[rand.Next(0, AllPlayers.Count)];
                AllPlayers.Remove(player);
                Main.PlayerStates[player.PlayerId].MainRole = role;

                var selfRole = player.PlayerId == hostId ? hostBaseRole : BaseRole;
                var othersRole = player.PlayerId == hostId ? RoleTypes.Crewmate : RoleTypes.Scientist;

                //去同步角色视角
                foreach (var target in Main.AllPlayerControls)
                {
                    if (player.PlayerId != target.PlayerId)
                    {
                        rolesMap[(player.PlayerId, target.PlayerId)] = othersRole;
                    }
                    else
                    {
                        rolesMap[(player.PlayerId, target.PlayerId)] = selfRole;
                    }
                }

                //他者視点
                foreach (var seer in Main.AllPlayerControls)
                {
                    if (player.PlayerId != seer.PlayerId)
                    {
                        rolesMap[(seer.PlayerId, player.PlayerId)] = othersRole;
                    }
                }
                RpcSetRoleReplacer.OverriddenSenderList.Add(senders[player.PlayerId]);
                //ホスト視点はロール決定
                player.SetRole(othersRole);
                player.Data.IsDead = true;
            }
        }
        public static void MakeDesyncSender(Dictionary<byte, CustomRpcSender> senders, Dictionary<(byte, byte), RoleTypes> rolesMap)
        {
            var hostId = PlayerControl.LocalPlayer.PlayerId;
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

        private static List<PlayerControl> AssignCustomRolesFromList(int ID, CustomRoles role, List<PlayerControl> players, int RawCount = -1)
        {
            if (players == null || players.Count <= 0) return null;
            var rand = IRandom.Instance;
            var count = Math.Clamp(RawCount, 0, players.Count);
            if (RawCount == -1) count = Math.Clamp(role.GetCount(), 0, players.Count);
            if (count <= 0) return null;
            if (CustomRolesHelper.IsNK(role) && Main.assignedNK >= Main.needOfNK) return null;
            if (CustomRolesHelper.IsNNK(role) && Main.assignedNNK >= Main.needOfNNK) return null;
            if (CustomRolesHelper.IsNK(role)) Main.assignedNK++;
            if (CustomRolesHelper.IsNNK(role)) Main.assignedNNK++;
            if (ID != -1) Main.funList.Remove(ID);
            List<PlayerControl> AssignedPlayers = new();
            SetColorPatch.IsAntiGlitchDisabled = true;
            for (var i = 0; i < count; i++)
            {
                var player = players[rand.Next(0, players.Count)];
                AssignedPlayers.Add(player);
                players.Remove(player);
                Main.PlayerStates[player.PlayerId].MainRole = role;
                Logger.Info("役職設定:" + player?.Data?.PlayerName + " = " + role.ToString(), "AssignRoles");

                if (Options.CurrentGameMode == CustomGameMode.HideAndSeek)
                {
                    if (player.Is(CustomRoles.HASTroll))
                        player.RpcSetColor(2);
                    else if (player.Is(CustomRoles.HASFox))
                        player.RpcSetColor(3);
                }
            }
            SetColorPatch.IsAntiGlitchDisabled = false;
            return AssignedPlayers;
        }

        private static void AssignLoversRolesFromList()
        {
            if (CustomRoles.Lovers.IsEnable())
            {
                //初始化恋人
                Main.LoversPlayers.Clear();
                Main.isLoversDead = false;
                //随机选择两人
                AssignLoversRoles(2);
            }
        }
        private static void AssignLoversRoles(int RawCount = -1)
        {
            var allPlayers = new List<PlayerControl>();
            foreach (var player in Main.AllPlayerControls)
            {
                if (player.Is(CustomRoles.GM)) continue;
                allPlayers.Add(player);
            }
            var loversRole = CustomRoles.Lovers;
            var rand = IRandom.Instance;
            var count = Math.Clamp(RawCount, 0, allPlayers.Count);
            if (RawCount == -1) count = Math.Clamp(loversRole.GetCount(), 0, allPlayers.Count);
            if (count <= 0) return;

            for (var i = 0; i < count; i++)
            {
                var player = allPlayers[rand.Next(0, allPlayers.Count)];
                Main.LoversPlayers.Add(player);
                allPlayers.Remove(player);
                Main.PlayerStates[player.PlayerId].SetSubRole(loversRole);
                Logger.Info("役職設定:" + player?.Data?.PlayerName + " = " + player.GetCustomRole().ToString() + " + " + loversRole.ToString(), "AssignLovers");
            }
            RPC.SyncLoversPlayers();
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSetRole))]
        class RpcSetRoleReplacer
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
}