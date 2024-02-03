using AmongUs.Data;
using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;
using InnerNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TONX.Modules;
using TONX.Roles.AddOns.Impostor;
using TONX.Roles.Core;
using TONX.Roles.Core.Interfaces;
using TONX.Roles.Impostor;
using TONX.Roles.Neutral;
using UnityEngine;
using static TONX.Translator;

namespace TONX;

static class ExtendedPlayerControl
{
    public static void RpcSetCustomRole(this PlayerControl player, CustomRoles role)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (player.Is(role)) return;

        if (role < CustomRoles.NotAssigned)
        {
            player.GetRoleClass()?.Dispose();
            PlayerState.GetByPlayerId(player.PlayerId).SetMainRole(role);
        }
        else if (role >= CustomRoles.NotAssigned)   //500:NoSubRole 501~:SubRole
        {
            PlayerState.GetByPlayerId(player.PlayerId).SetSubRole(role);
        }
        CustomRoleManager.CreateInstance(role, player);

        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetCustomRole, SendOption.Reliable, -1);
        writer.Write(player.PlayerId);
        writer.WritePacked((int)role);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void RpcSetCustomRole(byte PlayerId, CustomRoles role)
    {
        if (!AmongUsClient.Instance.AmHost) return;

        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetCustomRole, SendOption.Reliable, -1);
        writer.Write(PlayerId);
        writer.WritePacked((int)role);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    public static void RpcExile(this PlayerControl player)
    {
        RPC.ExileAsync(player);
    }
    public static ClientData GetClient(this PlayerControl player)
    {
        try
        {
            var client = AmongUsClient.Instance.allClients.ToArray().Where(cd => cd.Character.PlayerId == player.PlayerId).FirstOrDefault();
            return client;
        }
        catch
        {
            return null;
        }
    }
    public static int GetClientId(this PlayerControl player)
    {
        if (player == null) return -1;
        var client = player.GetClient();
        return client == null ? -1 : client.Id;
    }
    public static CustomRoles GetCustomRole(this GameData.PlayerInfo player)
    {
        return player == null || player.Object == null ? CustomRoles.Crewmate : player.Object.GetCustomRole();
    }
    /// <summary>
    /// ※サブロールは取得できません。
    /// </summary>
    public static CustomRoles GetCustomRole(this PlayerControl player)
    {
        if (player == null)
        {
            var caller = new System.Diagnostics.StackFrame(1, false);
            var callerMethod = caller.GetMethod();
            string callerMethodName = callerMethod.Name;
            string callerClassName = callerMethod.DeclaringType.FullName;
            Logger.Warn(callerClassName + "." + callerMethodName + "がCustomRoleを取得しようとしましたが、対象がnullでした。", "GetCustomRole");
            return CustomRoles.Crewmate;
        }
        var state = PlayerState.GetByPlayerId(player.PlayerId);

        return state?.MainRole ?? CustomRoles.Crewmate;
    }

    public static List<CustomRoles> GetCustomSubRoles(this PlayerControl player)
    {
        if (player == null)
        {
            Logger.Warn("CustomSubRoleを取得しようとしましたが、対象がnullでした。", "getCustomSubRole");
            return new() { CustomRoles.NotAssigned };
        }
        return PlayerState.GetByPlayerId(player.PlayerId).SubRoles;
    }
    public static CountTypes GetCountTypes(this PlayerControl player)
    {
        if (player == null)
        {
            var caller = new System.Diagnostics.StackFrame(1, false);
            var callerMethod = caller.GetMethod();
            string callerMethodName = callerMethod.Name;
            string callerClassName = callerMethod.DeclaringType.FullName;
            Logger.Warn(callerClassName + "." + callerMethodName + "がCountTypesを取得しようとしましたが、対象がnullでした。", "GetCountTypes");
            return CountTypes.None;
        }

        return PlayerState.GetByPlayerId(player.PlayerId)?.CountType ?? CountTypes.None;
    }
    public static void RpcSetNameEx(this PlayerControl player, string name)
    {
        foreach (var seer in Main.AllPlayerControls)
        {
            Main.LastNotifyNames[(player.PlayerId, seer.PlayerId)] = name;
        }
        HudManagerPatch.LastSetNameDesyncCount++;

        Logger.Info($"Set:{player?.Data?.PlayerName}:{name} for All", "RpcSetNameEx");
        player.RpcSetName(name);
    }

    public static void RpcSetNamePrivate(this PlayerControl player, string name, bool DontShowOnModdedClient = false, PlayerControl seer = null, bool force = false)
    {
        //player: 名前の変更対象
        //seer: 上の変更を確認することができるプレイヤー
        if (player == null || name == null || !AmongUsClient.Instance.AmHost) return;
        if (seer == null) seer = player;
        if (!force && Main.LastNotifyNames[(player.PlayerId, seer.PlayerId)] == name)
        {
            //Logger.info($"Cancel:{player.name}:{name} for {seer.name}", "RpcSetNamePrivate");
            return;
        }
        Main.LastNotifyNames[(player.PlayerId, seer.PlayerId)] = name;
        HudManagerPatch.LastSetNameDesyncCount++;
        Logger.Info($"Set: {player?.Data?.PlayerName} => {name} for {seer.GetNameWithRole()}", "RpcSetNamePrivate");

        var clientId = seer.GetClientId();
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(player.NetId, (byte)RpcCalls.SetName, SendOption.Reliable, clientId);
        writer.Write(name);
        writer.Write(DontShowOnModdedClient);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void RpcSetRoleDesync(this PlayerControl player, RoleTypes role, int clientId)
    {
        //player: 名前の変更対象

        if (player == null) return;
        if (AmongUsClient.Instance.ClientId == clientId)
        {
            player.SetRole(role);
            return;
        }
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(player.NetId, (byte)RpcCalls.SetRole, SendOption.Reliable, clientId);
        writer.Write((ushort)role);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    public static void SetKillCooldown(this PlayerControl player, float time = -1f)
    {
        if (player == null) return;
        if (!player.CanUseKillButton()) return;
        if (time >= 0f) Main.AllPlayerKillCooldown[player.PlayerId] = time * 2;
        else Main.AllPlayerKillCooldown[player.PlayerId] *= 2;
        player.SyncSettings();
        player.RpcProtectedMurderPlayer();
        player.ResetKillCooldown();
    }
    public static void SetKillCooldownV2(this PlayerControl player, float time = -1f, PlayerControl target = null, bool forceAnime = false)
    {
        if (player == null) return;
        if (!player.CanUseKillButton()) return;
        if (target == null) target = player;
        if (time >= 0f) Main.AllPlayerKillCooldown[player.PlayerId] = time * 2;
        else Main.AllPlayerKillCooldown[player.PlayerId] *= 2;
        if (forceAnime || !player.IsModClient())
        {
            player.SyncSettings();
            player.RpcProtectedMurderPlayer(target);
        }
        else
        {
            time = Main.AllPlayerKillCooldown[player.PlayerId] / 2;
            if (player.AmOwner) PlayerControl.LocalPlayer.SetKillTimer(time);
            else
            {
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetKillTimer, SendOption.Reliable, player.GetClientId());
                writer.Write(time);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
            }
            if (!MeetingStates.FirstMeeting)
                Main.AllPlayerControls.Where(x => x.Is(CustomRoles.Observer) && target.PlayerId != x.PlayerId).Do(x => x.RpcProtectedMurderPlayer(target));
        }
        player.ResetKillCooldown();
    }
    public static void RpcSpecificMurderPlayer(this PlayerControl killer, PlayerControl target = null)
    {
        if (target == null) target = killer;
        if (killer.AmOwner)
        {
            killer.MurderPlayer(target);
        }
        else
        {
            MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(killer.NetId, (byte)RpcCalls.MurderPlayer, SendOption.Reliable, killer.GetClientId());
            messageWriter.WriteNetObject(target);
            AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
        }
    }
    [Obsolete]
    public static void RpcSpecificProtectPlayer(this PlayerControl killer, PlayerControl target = null, int colorId = 0)
    {
        if (AmongUsClient.Instance.AmClient)
        {
            killer.ProtectPlayer(target, colorId);
        }
        MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(killer.NetId, (byte)RpcCalls.ProtectPlayer, SendOption.Reliable, killer.GetClientId());
        messageWriter.WriteNetObject(target);
        messageWriter.Write(colorId);
        AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
    }
    public static void RpcResetAbilityCooldown(this PlayerControl target)
    {
        if (target == null || !AmongUsClient.Instance.AmHost)
        {
            return;
        }
        Logger.Info($"アビリティクールダウンのリセット:{target.name}({target.PlayerId})", "RpcResetAbilityCooldown");
        if (PlayerControl.LocalPlayer == target)
        {
            //targetがホストだった場合
            PlayerControl.LocalPlayer.Data.Role.SetCooldown();
        }
        else
        {
            //targetがホスト以外だった場合
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(target.NetId, (byte)RpcCalls.ProtectPlayer, SendOption.None, target.GetClientId());
            writer.WriteNetObject(target);
            writer.Write(0);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        /*
            プレイヤーがバリアを張ったとき、そのプレイヤーの役職に関わらずアビリティーのクールダウンがリセットされます。
            ログの追加により無にバリアを張ることができなくなったため、代わりに自身に0秒バリアを張るように変更しました。
            この変更により、役職としての守護天使が無効化されます。
            ホストのクールダウンは直接リセットします。
        */
    }
    public static void RpcDesyncUpdateSystem(this PlayerControl target, SystemTypes systemType, int amount)
    {
        MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(ShipStatus.Instance.NetId, (byte)RpcCalls.UpdateSystem, SendOption.Reliable, target.GetClientId());
        messageWriter.Write((byte)systemType);
        messageWriter.WriteNetObject(target);
        messageWriter.Write((byte)amount);
        AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
    }

    /*public static void RpcBeKilled(this PlayerControl player, PlayerControl KilledBy = null) {
        if(!AmongUsClient.Instance.AmHost) return;
        byte KilledById;
        if(KilledBy == null)
            KilledById = byte.MaxValue;
        else
            KilledById = KilledBy.PlayerId;

        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(player.NetId, (byte)CustomRPC.BeKilled, SendOption.Reliable, -1);
        writer.Write(player.PlayerId);
        writer.Write(KilledById);
        AmongUsClient.Instance.FinishRpcImmediately(writer);

        RPC.BeKilled(player.PlayerId, KilledById);
    }*/
    public static void MarkDirtySettings(this PlayerControl player)
    {
        PlayerGameOptionsSender.SetDirty(player.PlayerId);
    }
    public static void SyncSettings(this PlayerControl player)
    {
        PlayerGameOptionsSender.SetDirty(player.PlayerId);
        GameOptionsSender.SendAllGameOptions();
    }
    public static TaskState GetPlayerTaskState(this PlayerControl player)
    {
        return PlayerState.GetByPlayerId(player.PlayerId).GetTaskState();
    }

    /*public static GameOptionsData DeepCopy(this GameOptionsData opt)
    {
        var optByte = opt.ToBytes(5);
        return GameOptionsData.FromBytes(optByte);
    }*/

    public static string GetTrueRoleName(this PlayerControl player)
    {
        return Utils.GetTrueRoleName(player.PlayerId);
    }
    public static string GetSubRoleName(this PlayerControl player, bool forUser)
    {
        var SubRoles = PlayerState.GetByPlayerId(player.PlayerId).SubRoles;
        if (SubRoles.Count == 0) return "";
        var sb = new StringBuilder();
        foreach (var role in SubRoles)
        {
            if (role is CustomRoles.NotAssigned) continue;
            sb.Append($"{Utils.ColorString(Color.white, " + ")}{Utils.GetRoleName(role, forUser)}");
        }

        return sb.ToString();
    }
    public static string GetAllRoleName(this PlayerControl player)
    {
        if (!player) return null;
        var text = Utils.GetRoleName(player.GetCustomRole());
        text += player.GetSubRoleName(false);
        return text;
    }
    public static string GetNameWithRole(this PlayerControl player, bool forUser = false)
    {
        var ret = $"{player?.Data?.PlayerName}" + (GameStates.IsInGame ? $"({player?.GetAllRoleName()})" : "");
        return (forUser ? ret : ret.RemoveHtmlTags());
    }
    public static string GetRoleColorCode(this PlayerControl player)
    {
        return Utils.GetRoleColorCode(player.GetCustomRole());
    }
    public static Color GetRoleColor(this PlayerControl player)
    {
        return Utils.GetRoleColor(player.GetCustomRole());
    }
    public static void ResetPlayerCam(this PlayerControl pc, float delay = 0f)
    {
        if (pc == null || !AmongUsClient.Instance.AmHost || pc.AmOwner) return;

        var systemtypes = Utils.GetCriticalSabotageSystemType();

        _ = new LateTask(() =>
        {
            pc.RpcDesyncUpdateSystem(systemtypes, 128);
        }, 0f + delay, "Reactor Desync");

        _ = new LateTask(() =>
        {
            pc.RpcSpecificMurderPlayer();
        }, 0.2f + delay, "Murder To Reset Cam");

        _ = new LateTask(() =>
        {
            pc.RpcDesyncUpdateSystem(systemtypes, 16);
            if (Main.NormalOptions.MapId == 4) //Airship用
                pc.RpcDesyncUpdateSystem(systemtypes, 17);
        }, 0.4f + delay, "Fix Desync Reactor");
    }
    public static void ReactorFlash(this PlayerControl pc, float delay = 0f)
    {
        if (pc == null) return;
        int clientId = pc.GetClientId();
        // Logger.Info($"{pc}", "ReactorFlash");
        var systemtypes = Utils.GetCriticalSabotageSystemType();
        float FlashDuration = Options.KillFlashDuration.GetFloat();

        pc.RpcDesyncUpdateSystem(systemtypes, 128);

        _ = new LateTask(() =>
        {
            pc.RpcDesyncUpdateSystem(systemtypes, 16);
            if (Main.NormalOptions.MapId == 4) //Airship用
                pc.RpcDesyncUpdateSystem(systemtypes, 17);
        }, FlashDuration + delay, "Fix Desync Reactor");
    }

    public static string GetTrueName(this PlayerControl player)
    {
        return Main.AllPlayerNames.TryGetValue(player.PlayerId, out var name) ? name : GetRealName(player, GameStates.IsMeeting);
    }
    public static string GetRealName(this PlayerControl player, bool isMeeting = false)
    {
        return isMeeting ? player?.Data?.PlayerName : player?.name;
    }
    public static bool CanUseKillButton(this PlayerControl pc)
    {
        if (!pc.IsAlive() || pc.Data.Role.Role == RoleTypes.GuardianAngel || pc.IsEaten()) return false;

        var roleCanUse = (pc.GetRoleClass() as IKiller)?.CanUseKillButton();

        return roleCanUse ?? pc.Is(CustomRoleTypes.Impostor);
    }
    public static bool CanUseImpostorVentButton(this PlayerControl pc)
    {
        if (!pc.IsAlive()) return false;

        var roleCanUse = (pc.GetRoleClass() as IKiller)?.CanUseImpostorVentButton();

        return roleCanUse ?? false;
    }
    public static bool CanUseSabotageButton(this PlayerControl pc)
    {
        var roleCanUse = (pc.GetRoleClass() as IKiller)?.CanUseSabotageButton();

        return roleCanUse ?? false;
    }
    public static void ResetKillCooldown(this PlayerControl player)
    {
        Main.AllPlayerKillCooldown[player.PlayerId] = (player.GetRoleClass() as IKiller)?.CalculateKillCooldown() ?? Options.DefaultKillCooldown; //キルクールをデフォルトキルクールに変更
        if (player.PlayerId == LastImpostor.currentId)
            LastImpostor.SetKillCooldown();
    }
    public static void RpcExileV2(this PlayerControl player)
    {
        player.Exiled();
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(player.NetId, (byte)RpcCalls.Exiled, SendOption.None, -1);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void MurderPlayer(this PlayerControl killer, PlayerControl target)
    {
        killer.MurderPlayer(target, SucceededFlags);
    }
    public const MurderResultFlags SucceededFlags = MurderResultFlags.Succeeded | MurderResultFlags.DecisionByHost;
    public static void RpcMurderPlayer(this PlayerControl killer, PlayerControl target)
    {
        killer.RpcMurderPlayer(target, true);
    }
    public static void RpcMurderPlayerV2(this PlayerControl killer, PlayerControl target)
    {
        if (target == null) target = killer;
        if (AmongUsClient.Instance.AmClient)
        {
            killer.MurderPlayer(target);
        }
        MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(killer.NetId, (byte)RpcCalls.MurderPlayer, SendOption.None, -1);
        messageWriter.WriteNetObject(target);
        messageWriter.Write((int)SucceededFlags);
        AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
        Utils.NotifyRoles();
    }
    public static void RpcProtectedMurderPlayer(this PlayerControl killer, PlayerControl target = null)
    {
        //killerが死んでいる場合は実行しない
        if (!killer.IsAlive()) return;

        if (target == null) target = killer;
        // Host
        if (killer.AmOwner)
        {
            killer.MurderPlayer(target, MurderResultFlags.FailedProtected);
        }
        // Other Clients
        if (killer.PlayerId != 0)
        {
            var writer = AmongUsClient.Instance.StartRpcImmediately(killer.NetId, (byte)RpcCalls.MurderPlayer, SendOption.Reliable);
            writer.WriteNetObject(target);
            writer.Write((int)MurderResultFlags.FailedProtected);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
    }
    public static void RpcSuicideWithAnime(this PlayerControl pc, bool fromHost = false)
    {
        if (!fromHost && !AmongUsClient.Instance.AmHost) return;
        var amOwner = pc.AmOwner;
        if (AmongUsClient.Instance.AmHost)
        {
            pc.Data.IsDead = true;
            pc.RpcExileV2();
            PlayerState.GetByPlayerId(pc.PlayerId)?.SetDead();

            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SuicideWithAnime, SendOption.Reliable, -1);
            writer.Write(pc.PlayerId);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        var meetingHud = MeetingHud.Instance;
        var hudManager = DestroyableSingleton<HudManager>.Instance;
        SoundManager.Instance.PlaySound(pc.KillSfx, false, 0.8f);
        hudManager.KillOverlay.ShowKillAnimation(pc.Data, pc.Data);
        if (amOwner)
        {
            hudManager.ShadowQuad.gameObject.SetActive(false);
            pc.nameText().GetComponent<MeshRenderer>().material.SetInt("_Mask", 0);
            pc.RpcSetScanner(false);
            ImportantTextTask importantTextTask = new GameObject("_Player").AddComponent<ImportantTextTask>();
            importantTextTask.transform.SetParent(AmongUsClient.Instance.transform, false);
            meetingHud.SetForegroundForDead();
        }
        PlayerVoteArea voteArea = MeetingHud.Instance.playerStates.First(
            x => x.TargetPlayerId == pc.PlayerId
        );
        if (voteArea == null) return;
        if (voteArea.DidVote) voteArea.UnsetVote();
        voteArea.AmDead = true;
        voteArea.Overlay.gameObject.SetActive(true);
        voteArea.Overlay.color = Color.white;
        voteArea.XMark.gameObject.SetActive(true);
        voteArea.XMark.transform.localScale = Vector3.one;
        foreach (var playerVoteArea in meetingHud.playerStates)
        {
            if (playerVoteArea.VotedFor != pc.PlayerId) continue;
            playerVoteArea.UnsetVote();
            var voteAreaPlayer = Utils.GetPlayerById(playerVoteArea.TargetPlayerId);
            if (!voteAreaPlayer.AmOwner) continue;
            meetingHud.ClearVote();
        }
    }
    public static void NoCheckStartMeeting(this PlayerControl reporter, GameData.PlayerInfo target, bool force = false)
    { /*サボタージュ中でも関係なしに会議を起こせるメソッド
        targetがnullの場合はボタンとなる*/

        if (GameStates.IsMeeting) return;
        if (Options.DisableMeeting.GetBool()) return;
        Logger.Info($"{reporter.GetNameWithRole()} => {target?.Object?.GetNameWithRole() ?? "null"}", "NoCheckStartMeeting");

        foreach (var role in CustomRoleManager.AllActiveRoles.Values)
        {
            role.OnReportDeadBody(reporter, target);
        }

        Main.AllPlayerControls
                    .Where(pc => Main.CheckShapeshift.ContainsKey(pc.PlayerId))
                    .Do(pc => Camouflage.RpcSetSkin(pc, RevertToDefault: true));
        MeetingTimeManager.OnReportDeadBody();

        Utils.NotifyRoles(isForMeeting: true, NoCache: true);

        Utils.SyncAllSettings();

        MeetingRoomManager.Instance.AssignSelf(reporter, target);
        DestroyableSingleton<HudManager>.Instance.OpenMeetingRoom(reporter);
        reporter.RpcStartMeeting(target);
    }
    public static bool IsModClient(this PlayerControl player) => Main.playerVersion.ContainsKey(player.PlayerId);
    ///<summary>
    ///プレイヤーのRoleBehaviourのGetPlayersInAbilityRangeSortedを実行し、戻り値を返します。
    ///</summary>
    ///<param name="ignoreColliders">trueにすると、壁の向こう側のプレイヤーが含まれるようになります。守護天使用</param>
    ///<returns>GetPlayersInAbilityRangeSortedの戻り値</returns>
    public static List<PlayerControl> GetPlayersInAbilityRangeSorted(this PlayerControl player, bool ignoreColliders = false) => GetPlayersInAbilityRangeSorted(player, pc => true, ignoreColliders);
    ///<summary>
    ///プレイヤーのRoleBehaviourのGetPlayersInAbilityRangeSortedを実行し、predicateの条件に合わないものを除外して返します。
    ///</summary>
    ///<param name="predicate">リストに入れるプレイヤーの条件 このpredicateに入れてfalseを返すプレイヤーは除外されます。</param>
    ///<param name="ignoreColliders">trueにすると、壁の向こう側のプレイヤーが含まれるようになります。守護天使用</param>
    ///<returns>GetPlayersInAbilityRangeSortedの戻り値から条件に合わないプレイヤーを除外したもの。</returns>
    public static List<PlayerControl> GetPlayersInAbilityRangeSorted(this PlayerControl player, Predicate<PlayerControl> predicate, bool ignoreColliders = false)
    {
        var rangePlayersIL = RoleBehaviour.GetTempPlayerList();
        List<PlayerControl> rangePlayers = new();
        player.Data.Role.GetPlayersInAbilityRangeSorted(rangePlayersIL, ignoreColliders);
        foreach (var pc in rangePlayersIL)
        {
            if (predicate(pc)) rangePlayers.Add(pc);
        }
        return rangePlayers;
    }
    public static bool IsImp(this PlayerControl player) => player.Is(CustomRoleTypes.Impostor);
    public static bool IsCrew(this PlayerControl player) => player.Is(CustomRoleTypes.Crewmate);
    public static bool IsCrewKiller(this PlayerControl player) => player.Is(CustomRoleTypes.Crewmate) && ((CustomRoleManager.GetByPlayerId(player.PlayerId) as IKiller)?.IsKiller ?? false);
    public static bool IsCrewNonKiller(this PlayerControl player) => player.Is(CustomRoleTypes.Crewmate) && !player.IsCrewKiller();
    public static bool IsNeutral(this PlayerControl player) => player.Is(CustomRoleTypes.Neutral);
    public static bool IsNeutralKiller(this PlayerControl player) => player.Is(CustomRoleTypes.Neutral) && ((CustomRoleManager.GetByPlayerId(player.PlayerId) as IKiller)?.IsKiller ?? false);
    public static bool IsNeutralNonKiller(this PlayerControl player) => player.Is(CustomRoleTypes.Neutral) && !player.IsNeutralKiller();
    public static bool IsNeutralEvil(this PlayerControl player) => player.Is(CustomRoleTypes.Neutral) && player is IAdditionalWinner;
    public static bool IsNeutralBenign(this PlayerControl player) => player.Is(CustomRoleTypes.Neutral) && player is not IAdditionalWinner;
    public static bool IsShapeshifting(this PlayerControl player) => Main.CheckShapeshift.TryGetValue(player.PlayerId, out bool ss) && ss;
    public static bool KnowDeathReason(this PlayerControl seer, PlayerControl seen)
    {
        // targetが生きてたらfalse
        if (seen.IsAlive())
        {
            return false;
        }
        // seerが死亡済で，霊界から死因が見える設定がON
        if (!seer.IsAlive() && Options.GhostCanSeeDeathReason.GetBool())
        {
            return true;
        }

        // 役職による仕分け
        if (seer.GetRoleClass() is IDeathReasonSeeable deathReasonSeeable)
        {
            return deathReasonSeeable.CheckSeeDeathReason(seen);
        }
        return false;
    }
    public static string GetRoleInfo(this PlayerControl player, bool InfoLong = false)
    {
        var roleClass = player.GetRoleClass();
        var role = player.GetCustomRole();
        if (role is CustomRoles.Crewmate or CustomRoles.Impostor)
            InfoLong = false;

        var text = role.ToString();

        var Prefix = "";
        if (!InfoLong)
            switch (role)
            {
                case CustomRoles.Mafia:
                    if (roleClass is not Mafia mafia) break;

                    Prefix = mafia.CanUseKillButton() ? "After" : "Before";
                    break;
            };
        var Info = (role.IsVanilla() ? "Blurb" : "Info") + (InfoLong ? "Long" : "");
        return GetString($"{Prefix}{text}{Info}");
    }
    public static string GetRoleInfoByRole(this CustomRoles role, bool InfoLong = false)
    {
        if (role is CustomRoles.Crewmate or CustomRoles.Impostor)
            InfoLong = false;

        var text = role.ToString();

        var Prefix = "";

        var Info = (role.IsVanilla() ? "Blurb" : "Info") + (InfoLong ? "Long" : "");
        return GetString($"{Prefix}{text}{Info}");
    }
    public static void SetDeathReason(this PlayerControl target, CustomDeathReason reason)
    {
        if (target == null)
        {
            Logger.Info("target=null", "SetDeathReason");
            return;
        }
        var State = PlayerState.GetByPlayerId(target.PlayerId);
        State.DeathReason = reason;
    }
    public static void SetRealKiller(this PlayerControl target, byte killerId, bool NotOverRide = false)
    {
        if (target == null)
        {
            Logger.Info("target=null", "SetRealKiller");
            return;
        }
        var State = PlayerState.GetByPlayerId(target.PlayerId);
        if (State.RealKiller.Item1 != DateTime.MinValue && NotOverRide) return; //既に値がある場合上書きしない
        RPC.SetRealKiller(target.PlayerId, killerId);
    }
    public static void SetRealKiller(this PlayerControl target, PlayerControl killer, bool NotOverRide = false)
    {
        if (target == null)
        {
            Logger.Info("target=null", "SetRealKiller");
            return;
        }
        if (killer == null)
        {
            Logger.Info("killer=null", "SetRealKiller");
            return;
        }
        var State = PlayerState.GetByPlayerId(target.PlayerId);
        if (State.RealKiller.Item1 != DateTime.MinValue && NotOverRide) return; //既に値がある場合上書きしない
        RPC.SetRealKiller(target.PlayerId, killer.PlayerId);
    }
    public static PlayerControl GetRealKiller(this PlayerControl target)
    {
        var killerId = PlayerState.GetByPlayerId(target.PlayerId).GetRealKiller();
        return killerId == byte.MaxValue ? null : Utils.GetPlayerById(killerId);
    }
    public static PlainShipRoom GetPlainShipRoom(this PlayerControl pc)
    {
        if (!pc.IsAlive() || pc.IsEaten()) return null;
        var Rooms = ShipStatus.Instance.AllRooms;
        if (Rooms == null) return null;
        foreach (var room in Rooms)
        {
            if (!room.roomArea) continue;
            if (pc.Collider.IsTouching(room.roomArea))
                return room;
        }
        return null;
    }
    public static bool IsProtected(this PlayerControl self) => self.protectedByGuardianId > -1;

    //汎用
    public static bool Is(this PlayerControl target, CustomRoles role) =>
        role > CustomRoles.NotAssigned ? target.GetCustomSubRoles().Contains(role) : target.GetCustomRole() == role;
    public static bool Is(this PlayerControl target, CustomRoleTypes type) { return target.GetCustomRole().GetCustomRoleTypes() == type; }
    public static bool Is(this PlayerControl target, RoleTypes type) { return target.GetCustomRole().GetRoleTypes() == type; }
    public static bool Is(this PlayerControl target, CountTypes type) { return target.GetCountTypes() == type; }
    public static bool IsEaten(this PlayerControl target) => GameStates.IsInGame && Pelican.IsEaten(target.PlayerId);
    public static bool IsAlive(this PlayerControl target)
    {
        //ロビーなら生きている
        if (GameStates.IsLobby)
        {
            return true;
        }
        //targetがnullならば切断者なので生きていない
        if (target == null)
        {
            return false;
        }
        //目标为活死人
        if (target.Is(CustomRoles.Glitch))
        {
            return false;
        }
        //targetがnullでなく取得できない場合は登録前なので生きているとする
        if (PlayerState.GetByPlayerId(target.PlayerId) is not PlayerState state)
        {
            return true;
        }
        return !state.IsDead;
    }
}