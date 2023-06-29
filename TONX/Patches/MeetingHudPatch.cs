using HarmonyLib;
using Hazel;
using System.Collections.Generic;
using System.Text;
using TONX.Modules;
using TONX.Roles.AddOns.Common;
using TONX.Roles.Core;
using UnityEngine;
using static TONX.Translator;

namespace TONX;

[HarmonyPatch]
public static class MeetingHudPatch
{
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.CheckForEndVoting))]
    class CheckForEndVotingPatch
    {
        public static bool Prefix()
        {
            if (!AmongUsClient.Instance.AmHost) return true;
            MeetingVoteManager.Instance?.CheckAndEndMeeting();
            return false;
        }
    }
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.CastVote))]
    public static class CastVotePatch
    {
        public static bool Prefix(MeetingHud __instance, [HarmonyArgument(0)] byte srcPlayerId /* 投票者 */ , [HarmonyArgument(1)] byte suspectPlayerId /* 被票者 */ )
        {
            if (!AmongUsClient.Instance.AmHost) return true;

            var voter = Utils.GetPlayerById(srcPlayerId);
            if (voter != null)
            {
                //主动叛变模式
                if (Options.MadmateSpawnMode.GetInt() == 2 && srcPlayerId == suspectPlayerId)
                {
                    if (Main.MadmateNum < CustomRoles.Madmate.GetCount() && voter.CanBeMadmate())
                    {
                        Main.MadmateNum++;
                        voter.RpcSetCustomRole(CustomRoles.Madmate);
                        ExtendedPlayerControl.RpcSetCustomRole(voter.PlayerId, CustomRoles.Madmate);
                        Logger.Info($"注册附加职业：{voter.GetNameWithRole()} => {CustomRoles.Madmate}", "AssignCustomSubRoles");
                        voter.ShowPopUp(GetString("MadmateSelfVoteModeSuccessfulMutiny"));
                        Utils.SendMessage(GetString("MadmateSelfVoteModeSuccessfulMutiny"), voter.PlayerId);
                    }
                    else
                    {
                        voter.ShowPopUp(GetString("MadmateSelfVoteModeMutinyFailed"));
                        Utils.SendMessage(GetString("MadmateSelfVoteModeMutinyFailed"), voter.PlayerId);
                    }
                    ClearVote(__instance, voter);
                    return false;
                }
            }

            if (MeetingVoteManager.Instance.AddVote(srcPlayerId, suspectPlayerId)) return true;
            else
            {
                ClearVote(__instance, voter);
                return false;
            }
        }
        private static void ClearVote(MeetingHud hud, PlayerControl target)
        {
            new LateTask(() => { ClearVote(hud, target); }, 0.4f, "ClearVote First");
            new LateTask(() => { ClearVote(hud, target); }, 0.6f, "ClearVote Second");
            static void ClearVote(MeetingHud hud, PlayerControl target)
            {
                Logger.Info($"Clear Vote For: {target.GetNameWithRole()}", "ClearVote");
                MessageWriter writer = MessageWriter.Get(SendOption.Reliable);
                writer.StartMessage(6);
                writer.Write(AmongUsClient.Instance.GameId);
                writer.WritePacked(target.GetClientId());
                {
                    writer.StartMessage(2);
                    writer.WritePacked(hud.NetId);
                    writer.WritePacked((uint)RpcCalls.ClearVote);
                }
                AmongUsClient.Instance.FinishRpcImmediately(writer);
            }
        }
    }
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    class StartPatch
    {
        public static void Prefix()
        {
            Logger.Info("------------会议开始------------", "Phase");
            ChatUpdatePatch.DoBlockChat = true;
            GameStates.AlreadyDied |= !Utils.IsAllAlive;
            Main.AllPlayerControls.Do(x => ReportDeadBodyPatch.WaitReport[x.PlayerId].Clear());
            MeetingStates.MeetingCalled = true;
        }
        public static void Postfix(MeetingHud __instance)
        {
            MeetingVoteManager.Start();

            SoundManager.Instance.ChangeAmbienceVolume(0f);
            if (!GameStates.IsModHost) return;
            var myRole = PlayerControl.LocalPlayer.GetRoleClass();

            foreach (var pva in __instance.playerStates)
            {
                var pc = Utils.GetPlayerById(pva.TargetPlayerId);
                if (pc == null) continue;
                var roleTextMeeting = UnityEngine.Object.Instantiate(pva.NameText);
                roleTextMeeting.transform.SetParent(pva.NameText.transform);
                roleTextMeeting.transform.localPosition = new Vector3(0f, -0.18f, 0f);
                roleTextMeeting.fontSize = 1.5f;
                (roleTextMeeting.enabled, roleTextMeeting.text)
                        = Utils.GetRoleNameAndProgressTextData(PlayerControl.LocalPlayer, pc);
                roleTextMeeting.gameObject.name = "RoleTextMeeting";
                roleTextMeeting.enableWordWrapping = false;

                // 役職とサフィックスを同時に表示する必要が出たら要改修
                var suffixBuilder = new StringBuilder(32);
                if (myRole != null)
                {
                    suffixBuilder.Append(myRole.GetSuffix(PlayerControl.LocalPlayer, pc, isForMeeting: true));
                }
                suffixBuilder.Append(CustomRoleManager.GetSuffixOthers(PlayerControl.LocalPlayer, pc, isForMeeting: true));
                if (suffixBuilder.Length > 0)
                {
                    roleTextMeeting.text = suffixBuilder.ToString();
                    roleTextMeeting.enabled = true;
                }
            }

            if (Options.SyncButtonMode.GetBool())
            {
                Utils.SendMessage(string.Format(GetString("Message.SyncButtonLeft"), Options.SyncedButtonCount.GetFloat() - Options.UsedButtonCount));
                Logger.Info("紧急会议剩余 " + (Options.SyncedButtonCount.GetFloat() - Options.UsedButtonCount) + " 次使用次数", "SyncButtonMode");
            }
            if (AntiBlackout.OverrideExiledPlayer && !Options.NoGameEnd.GetBool())
            {
                new LateTask(() =>
                {
                    Utils.SendMessage(GetString("Warning.OverrideExiledPlayer"), 255, Utils.ColorString(Color.red, GetString("DefaultSystemMessageTitle")));
                }, 5f, "Warning OverrideExiledPlayer");
            }
            if (MeetingStates.FirstMeeting) TemplateManager.SendTemplate("OnFirstMeeting", noErr: true);
            TemplateManager.SendTemplate("OnMeeting", noErr: true);

            if (AmongUsClient.Instance.AmHost)
            {
                CustomRoleManager.AllActiveRoles.Values.Do(role => role.OnStartMeeting());
                MeetingStartNotify.OnMeetingStart();
                Brakar.OnMeetingStart();
            }

            if (AmongUsClient.Instance.AmHost)
            {
                _ = new LateTask(() =>
                {
                    foreach (var seer in Main.AllPlayerControls)
                    {
                        foreach (var target in Main.AllPlayerControls)
                        {
                            var seerName = seer.GetRealName(isMeeting: true);
                            var coloredName = Utils.ColorString(seer.GetRoleColor(), seerName);
                            seer.RpcSetNamePrivate(
                                seer == target ? coloredName : seerName,
                                true);
                        }
                    }
                    ChatUpdatePatch.DoBlockChat = false;
                }, 3f, "SetName To Chat");
            }

            foreach (var pva in __instance.playerStates)
            {
                if (pva == null) continue;
                var seer = PlayerControl.LocalPlayer;
                var seerRole = seer.GetRoleClass();

                var target = Utils.GetPlayerById(pva.TargetPlayerId);
                if (target == null) continue;

                var sb = new StringBuilder();

                //会議画面での名前変更
                //自分自身の名前の色を変更
                //NameColorManager準拠の処理
                pva.NameText.text = pva.NameText.text.ApplyNameColorData(seer, target, true);

                var overrideName = pva.NameText.text;
                //调用职业类通过 seer 重写 name
                seer.GetRoleClass()?.OverrideNameAsSeer(target, ref overrideName, true);
                //调用职业类通过 seen 重写 name
                target.GetRoleClass()?.OverrideNameAsSeen(seer, ref overrideName, true);
                pva.NameText.text = overrideName;

                //とりあえずSnitchは会議中にもインポスターを確認することができる仕様にしていますが、変更する可能性があります。

                if (seer.KnowDeathReason(target))
                    sb.Append($"({Utils.ColorString(Utils.GetRoleColor(CustomRoles.Doctor), Utils.GetVitalText(target.PlayerId))})");

                sb.Append(seerRole?.GetMark(seer, target, true));
                sb.Append(CustomRoleManager.GetMarkOthers(seer, target, true));

                bool isLover = false;
                foreach (var subRole in target.GetCustomSubRoles())
                {
                    switch (subRole)
                    {
                        case CustomRoles.Lovers:
                            if (seer.Is(CustomRoles.Lovers) || seer.Data.IsDead)
                            {
                                sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Lovers), "♡"));
                                isLover = true;
                            }
                            break;
                    }
                }

                //海王相关显示
                if ((seer.Is(CustomRoles.Ntr) || target.Is(CustomRoles.Ntr)) && !seer.Data.IsDead && !isLover)
                    sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Lovers), "♡"));
                else if (seer == target && CustomRoles.Ntr.Exist() && !isLover)
                    sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Lovers), "♡"));

                //会議画面ではインポスター自身の名前にSnitchマークはつけません。

                pva.NameText.text += sb.ToString();
                pva.ColorBlindName.transform.localPosition -= new Vector3(1.35f, 0f, 0f);
            }
        }
    }
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Update))]
    class UpdatePatch
    {
        public static void Postfix(MeetingHud __instance)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (Input.GetMouseButtonUp(1) && Input.GetKey(KeyCode.LeftControl))
            {
                __instance.playerStates.DoIf(x => x.HighlightedFX.enabled, x =>
                {
                    var player = Utils.GetPlayerById(x.TargetPlayerId);
                    player.RpcExileV2();
                    var state = PlayerState.GetByPlayerId(player.PlayerId);
                    state.DeathReason = CustomDeathReason.Execution;
                    state.SetDead();
                    Utils.SendMessage(string.Format(GetString("Message.Executed"), player.Data.PlayerName));
                    Logger.Info($"{player.GetNameWithRole()}を処刑しました", "Execution");
                    __instance.CheckForEndVoting();
                });
            }
        }
    }
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.OnDestroy))]
    class OnDestroyPatch
    {
        public static void Postfix()
        {
            MeetingStates.FirstMeeting = false;
            Logger.Info("------------会议结束------------", "Phase");
            if (AmongUsClient.Instance.AmHost)
            {
                AntiBlackout.SetIsDead();
                Main.AllPlayerControls.Do(pc => RandomSpawn.CustomNetworkTransformPatch.NumOfTP[pc.PlayerId] = 0);
                EAC.MeetingTimes = 0;
            }
            // MeetingVoteManagerを通さずに会議が終了した場合の後処理
            MeetingVoteManager.Instance?.Destroy();
        }
    }

    public static void TryAddAfterMeetingDeathPlayers(CustomDeathReason deathReason, params byte[] playerIds)
    {
        var AddedIdList = new List<byte>();
        foreach (var playerId in playerIds)
            if (Main.AfterMeetingDeathPlayers.TryAdd(playerId, deathReason))
                AddedIdList.Add(playerId);
        CheckForDeathOnExile(deathReason, AddedIdList.ToArray());
    }
    public static void CheckForDeathOnExile(CustomDeathReason deathReason, params byte[] playerIds)
    {
        foreach (var playerId in playerIds)
        {
            //Loversの後追い
            if (CustomRoles.Lovers.Exist(true) && !Main.isLoversDead && Main.LoversPlayers.Find(lp => lp.PlayerId == playerId) != null)
                FixedUpdatePatch.LoversSuicide(playerId, true);
        }
    }

    [HarmonyPatch(typeof(PlayerVoteArea), nameof(PlayerVoteArea.SetHighlighted))]
    class SetHighlightedPatch
    {
        public static bool Prefix(PlayerVoteArea __instance, bool value)
        {
            if (!AmongUsClient.Instance.AmHost) return true;
            if (!__instance.HighlightedFX) return false;
            __instance.HighlightedFX.enabled = value;
            return false;
        }
    }
}