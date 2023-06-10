using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using UnityEngine;
using Hazel;

using TOHE.Roles.Core;
using TOHE.Roles.Core.Interfaces;
using static TOHE.Translator;

namespace TOHE;

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.CheckForEndVoting))]
class CheckForEndVotingPatch
{
    public static bool Prefix(MeetingHud __instance)
    {
        if (!AmongUsClient.Instance.AmHost) return true;
        var voteLog = Logger.Handler("Vote");
        try
        {
            List<MeetingHud.VoterState> statesList = new();
            MeetingHud.VoterState[] states;
            foreach (var pva in __instance.playerStates)
            {
                if (pva == null) continue;
                PlayerControl pc = Utils.GetPlayerById(pva.TargetPlayerId);
                if (pc == null) continue;

                //主动叛变
                if (pva.DidVote && pc.PlayerId == pva.VotedFor && pva.VotedFor < 253 && !pc.Data.IsDead)
                {
                    if (Options.MadmateSpawnMode.GetInt() == 2 && Main.MadmateNum < CustomRoles.Madmate.GetCount() && pc.CanBeMadmate())
                    {
                        Main.MadmateNum++;
                        pc.RpcSetCustomRole(CustomRoles.Madmate);
                        ExtendedPlayerControl.RpcSetCustomRole(pc.PlayerId, CustomRoles.Madmate);
                        Utils.NotifyRoles(true, pc, true);
                        Logger.Info("设置职业:" + pc?.Data?.PlayerName + " = " + pc.GetCustomRole().ToString() + " + " + CustomRoles.Madmate.ToString(), "Assign " + CustomRoles.Madmate.ToString());
                    }
                }

                else if (pc.GetRoleClass()?.OnCheckForEndVoting(ref statesList, pva) == false)
                    return false;
            }
            foreach (var ps in __instance.playerStates)
            {
                //死んでいないプレイヤーが投票していない
                var state = PlayerState.GetByPlayerId(ps.TargetPlayerId);
                var pc = Utils.GetPlayerById(ps.TargetPlayerId);
                if (!(pc == null || state == null || !pc.IsAlive() || pc.Data == null || pc.Data.Disconnected || ps.DidVote)) return false;
            }

            GameData.PlayerInfo exiledPlayer = PlayerControl.LocalPlayer.Data;
            bool tie = false;

            for (var i = 0; i < __instance.playerStates.Length; i++)
            {
                PlayerVoteArea ps = __instance.playerStates[i];
                if (ps == null) continue;
                voteLog.Info(string.Format("{0,-2}{1}:{2,-3}{3}", ps.TargetPlayerId, Utils.PadRightV2($"({Utils.GetVoteName(ps.TargetPlayerId)})", 40), ps.VotedFor, $"({Utils.GetVoteName(ps.VotedFor)})"));
                var voter = Utils.GetPlayerById(ps.TargetPlayerId);
                if (voter == null || voter.Data == null || voter.Data.Disconnected) continue;
                if (Options.VoteMode.GetBool())
                {
                    if (ps.VotedFor == 253 && !voter.Data.IsDead && //スキップ
                        !(Options.WhenSkipVoteIgnoreFirstMeeting.GetBool() && MeetingStates.FirstMeeting) && //初手会議を除く
                        !(Options.WhenSkipVoteIgnoreNoDeadBody.GetBool() && !MeetingStates.IsExistDeadBody) && //死体がない時を除く
                        !(Options.WhenSkipVoteIgnoreEmergency.GetBool() && MeetingStates.IsEmergencyMeeting) //緊急ボタンを除く
                        )
                    {
                        switch (Options.GetWhenSkipVote())
                        {
                            case VoteMode.Suicide:
                                TryAddAfterMeetingDeathPlayers(CustomDeathReason.Suicide, ps.TargetPlayerId);
                                voteLog.Info($"{voter.GetNameWithRole()}因跳过投票自杀");
                                break;
                            case VoteMode.SelfVote:
                                ps.VotedFor = ps.TargetPlayerId;
                                voteLog.Info($"{voter.GetNameWithRole()}因跳过投票自票");
                                break;
                            default:
                                break;
                        }
                    }
                    if (ps.VotedFor == 254 && !voter.Data.IsDead)//無投票
                    {
                        switch (Options.GetWhenNonVote())
                        {
                            case VoteMode.Suicide:
                                TryAddAfterMeetingDeathPlayers(CustomDeathReason.Suicide, ps.TargetPlayerId);
                                voteLog.Info($"{voter.GetNameWithRole()}因未投票自杀");
                                break;
                            case VoteMode.SelfVote:
                                ps.VotedFor = ps.TargetPlayerId;
                                voteLog.Info($"{voter.GetNameWithRole()}因未投票自票");
                                break;
                            case VoteMode.Skip:
                                ps.VotedFor = 253;
                                voteLog.Info($"{voter.GetNameWithRole()}因未投票跳过");
                                break;
                            default:
                                break;
                        }
                    }
                }

                // 调用职业类在投票结束时对投票结果进行预处理
                if (voter.GetRoleClass()?.OnVotingEnd(ref statesList, ref ps) == false) continue;

                // 主动叛变模式下自票无效
                if (ps.TargetPlayerId == ps.VotedFor && Options.MadmateSpawnMode.GetInt() == 2) continue;

                statesList.Add(new MeetingHud.VoterState()
                {
                    VoterId = ps.TargetPlayerId,
                    VotedForId = ps.VotedFor
                });
            }
            states = statesList.ToArray();

            var VotingData = __instance.CustomCalculateVotes();
            byte exileId = byte.MaxValue;
            int max = 0;
            voteLog.Info("===决定驱逐玩家处理开始===");
            foreach (var data in VotingData)
            {
                voteLog.Info($"{data.Key}({Utils.GetVoteName(data.Key)}):{data.Value}票");
                if (data.Value > max)
                {
                    voteLog.Info(data.Key + "拥有更高票数(" + data.Value + ")");
                    exileId = data.Key;
                    max = data.Value;
                    tie = false;
                }
                else if (data.Value == max)
                {
                    voteLog.Info(data.Key + "与" + exileId + "的票数相同(" + data.Value + ")");
                    exileId = byte.MaxValue;
                    tie = true;
                }
                voteLog.Info($"驱逐ID: {exileId}, 最大: {max}票");
            }

            voteLog.Info($"决定驱逐玩家: {exileId}({Utils.GetVoteName(exileId)})");

            //TODO: FIXME: Braker
            //bool braked = false;
            //if (tie) //破平者判断
            //{
            //    byte target = byte.MaxValue;
            //    foreach (var data in VotingData.Where(x => x.Key < 15 && x.Value == max))
            //    {
            //        if (Main.BrakarVoteFor.Contains(data.Key))
            //        {
            //            if (target != byte.MaxValue)
            //            {
            //                target = byte.MaxValue;
            //                break;
            //            }
            //            target = data.Key;
            //        }
            //    }
            //    if (target != byte.MaxValue)
            //    {
            //        Logger.Info("破平者覆盖驱逐玩家", "Brakar Vote");
            //        exiledPlayer = Utils.GetPlayerInfoById(target);
            //        tie = false;
            //        braked = true;
            //    }
            //}

            //TODO: FIXME
            //Collector.CollectAmount(VotingData, __instance);

            if (Options.VoteMode.GetBool() && Options.WhenTie.GetBool() && tie)
            {
                switch ((TieMode)Options.WhenTie.GetValue())
                {
                    case TieMode.Default:
                        exiledPlayer = GameData.Instance.AllPlayers.ToArray().FirstOrDefault(info => info.PlayerId == exileId);
                        break;
                    case TieMode.All:
                        var exileIds = VotingData.Where(x => x.Key < 15 && x.Value == max).Select(kvp => kvp.Key).ToArray();
                        foreach (var playerId in exileIds)
                            Utils.GetPlayerById(playerId).SetRealKiller(null);
                        TryAddAfterMeetingDeathPlayers(CustomDeathReason.Vote, exileIds);
                        exiledPlayer = null;
                        break;
                    case TieMode.Random:
                        exiledPlayer = GameData.Instance.AllPlayers.ToArray().OrderBy(_ => Guid.NewGuid()).FirstOrDefault(x => VotingData.TryGetValue(x.PlayerId, out int vote) && vote == max);
                        tie = false;
                        break;
                }
            }
            else //if (!braked)
                exiledPlayer = GameData.Instance.AllPlayers.ToArray().FirstOrDefault(info => !tie && info.PlayerId == exileId);
            exiledPlayer?.Object.SetRealKiller(null);

            //RPC
            if (AntiBlackout.OverrideExiledPlayer)
            {
                __instance.RpcVotingComplete(states, null, true);
                ExileControllerWrapUpPatch.AntiBlackout_LastExiled = exiledPlayer;
            }
            else __instance.RpcVotingComplete(states, exiledPlayer, tie); //通常処理

            CheckForDeathOnExile(CustomDeathReason.Vote, exileId);

            Main.LastVotedPlayerInfo = exiledPlayer;
            if (Main.LastVotedPlayerInfo != null)
                ConfirmEjections(Main.LastVotedPlayerInfo);

            return false;
        }
        catch (Exception ex)
        {
            Logger.SendInGame(string.Format(GetString("Error.MeetingException"), ex.Message), true);
            throw;
        }
    }

    // 参考：https://github.com/music-discussion/TownOfHost-TheOtherRoles
    public static void ConfirmEjections(GameData.PlayerInfo exiledPlayer)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (exiledPlayer == null) return;
        var exileId = exiledPlayer.PlayerId;
        if (exileId is < 0 or > 254) return;
        var realName = exiledPlayer.Object.GetRealName(isMeeting: true);
        Main.LastVotedPlayer = realName;

        var player = Utils.GetPlayerById(exiledPlayer.PlayerId);
        var role = GetString(exiledPlayer.GetCustomRole().ToString());
        var crole = exiledPlayer.GetCustomRole();
        var coloredRole = Utils.GetTrueRoleName(exileId, false);
        var name = "";
        int impnum = 0;
        int neutralnum = 0;

        // 吟游诗人创作
        if (CustomRoles.Bard.Exist())
        {
            try { name = ModUpdater.Get("https://v1.hitokoto.cn/?encode=text"); }
            catch { name = GetString("ByBardGetFailed"); }
            name += "\n\t\t——" + GetString("ByBard");
            goto EndOfSession;
        }

        foreach (var pc in Main.AllAlivePlayerControls)
        {
            var pc_role = pc.GetCustomRole();
            if (pc_role.IsImpostor() && pc != exiledPlayer.Object)
                impnum++;
            else if (pc.IsNeutralKiller() && pc != exiledPlayer.Object)
                neutralnum++;
        }
        switch (Options.CEMode.GetInt())
        {
            case 0:
                name = string.Format(GetString("PlayerExiled"), realName);
                break;
            case 1:
                if (player.GetCustomRole().IsImpostor())
                    name = string.Format(GetString("BelongTo"), realName, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Impostor), GetString("TeamImpostor")));
                else if (player.GetCustomRole().IsCrewmate())
                    name = string.Format(GetString("IsGood"), realName);
                else if (player.GetCustomRole().IsNeutral())
                    name = string.Format(GetString("BelongTo"), realName, Utils.ColorString(new Color32(255, 171, 27, byte.MaxValue), GetString("TeamNeutral")));
                break;
            case 2:
                name = string.Format(GetString("PlayerIsRole"), realName, coloredRole);
                if (Options.ShowTeamNextToRoleNameOnEject.GetBool())
                {
                    name += " (";
                    if (player.GetCustomRole().IsImpostor() || player.Is(CustomRoles.Madmate))
                        name += Utils.ColorString(new Color32(255, 25, 25, byte.MaxValue), GetString("TeamImpostor"));
                    else if (player.GetCustomRole().IsNeutral() || player.Is(CustomRoles.Charmed))
                        name += Utils.ColorString(new Color32(255, 171, 27, byte.MaxValue), GetString("TeamNeutral"));
                    else if (player.GetCustomRole().IsCrewmate())
                        name += Utils.ColorString(new Color32(140, 255, 255, byte.MaxValue), GetString("TeamCrewmate"));
                    name += ")";
                }
                break;
        }
        var DecidedWinner = false;

        //TODO: FIXME

        if (DecidedWinner) name += "<size=0>";
        if (Options.ShowImpRemainOnEject.GetBool() && !DecidedWinner)
        {
            name += "\n";
            string comma = neutralnum > 0 ? "，" : "";
            if (impnum == 0) name += GetString("NoImpRemain") + comma;
            else name += string.Format(GetString("ImpRemain"), impnum) + comma;
            if (Options.ShowNKRemainOnEject.GetBool() && neutralnum > 0)
                name += string.Format(GetString("NeutralRemain"), neutralnum);
        }

    EndOfSession:

        name += "<size=0>";
        new LateTask(() =>
        {
            Main.DoBlockNameChange = true;
            if (GameStates.IsInGame) player.RpcSetName(name);
        }, 3.0f, "Change Exiled Player Name");
        new LateTask(() =>
        {
            if (GameStates.IsInGame && !player.Data.Disconnected)
            {
                player.RpcSetName(realName);
                Main.DoBlockNameChange = false;
            }
        }, 11.5f, "Change Exiled Player Name Back");
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
            //道連れチェック
            RevengeOnExile(playerId, deathReason);
        }
    }
    private static void RevengeOnExile(byte playerId, CustomDeathReason deathReason)
    {
        var player = Utils.GetPlayerById(playerId);
        if (player == null) return;
        var target = PickRevengeTarget(player, deathReason);
        if (target == null) return;
        TryAddAfterMeetingDeathPlayers(CustomDeathReason.Revenge, target.PlayerId);
        target.SetRealKiller(player);
        Logger.Info($"{player.GetNameWithRole()}の道連れ先:{target.GetNameWithRole()}", "RevengeOnExile");
    }
    private static PlayerControl PickRevengeTarget(PlayerControl exiledplayer, CustomDeathReason deathReason)//道連れ先選定
    {
        List<PlayerControl> TargetList = new();
        foreach (var candidate in Main.AllAlivePlayerControls)
        {
            if (candidate == exiledplayer || Main.AfterMeetingDeathPlayers.ContainsKey(candidate.PlayerId)) continue;
        }
        if (TargetList == null || TargetList.Count == 0) return null;
        var rand = IRandom.Instance;
        var target = TargetList[rand.Next(TargetList.Count)];
        return target;
    }
}

static class ExtendedMeetingHud
{
    public static Dictionary<byte, int> CustomCalculateVotes(this MeetingHud __instance)
    {
        Logger.Info("===计算票数处理开始===", "Vote");
        Dictionary<byte, int> dic = new();

        //| 投票された人 | 投票された回数 |
        for (int i = 0; i < __instance.playerStates.Length; i++)
        {
            PlayerVoteArea ps = __instance.playerStates[i]; //全部玩家
            if (ps == null) continue;
            if (ps.VotedFor is not 252 and not byte.MaxValue and not 254) //已投票
            {
                // 默认票数1票
                int VoteNum = 1;

                //调用职业类的票数计算函数
                CustomRoleManager.GetByPlayerId(ps.TargetPlayerId)?.OnCalculateVotes(ref ps, ref VoteNum);

                // 主动叛变模式下自票无效
                if (ps.TargetPlayerId == ps.VotedFor && Options.MadmateSpawnMode.GetInt() == 2) VoteNum = 0;

                //投票を1追加 キーが定義されていない場合は1で上書きして定義
                dic[ps.VotedFor] = !dic.TryGetValue(ps.VotedFor, out int num) ? VoteNum : num + VoteNum;//统计该玩家被投的数量
            }
        }
        return dic;
    }
}
[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
class MeetingHudStartPatch
{
    public static void NotifyOnMeetingStart()
    {
        if (!AmongUsClient.Instance.AmHost) return;

        List<(string, byte, string)> msgToSend = new();

        void AddMsg(string text, byte sendTo = 255, string title = "")
            => msgToSend.Add((text, sendTo, title));

        //首次会议技能提示
        if (Options.SendRoleDescriptionFirstMeeting.GetBool() && MeetingStates.FirstMeeting)
            foreach (var pc in Main.AllAlivePlayerControls.Where(x => !x.IsModClient()))
            {
                var role = pc.GetCustomRole();
                var sb = new StringBuilder();
                sb.Append(GetString(role.ToString()) + Utils.GetRoleDisplaySpawnMode(role) + pc.GetRoleInfo(true));
                if (Options.CustomRoleSpawnChances.TryGetValue(role, out var opt))
                    Utils.ShowChildrenSettings(opt, ref sb, command: true);
                var txt = sb.ToString();
                sb.Clear().Append(txt.RemoveHtmlTags());
                foreach (var subRole in PlayerState.AllPlayerStates[pc.PlayerId].SubRoles)
                    sb.Append($"\n\n" + GetString($"{subRole}") + Utils.GetRoleDisplaySpawnMode(subRole) + GetString($"{subRole}InfoLong"));
                if (CustomRoles.Ntr.Exist() && (role is not CustomRoles.GM and not CustomRoles.Ntr))
                    sb.Append($"\n\n" + GetString($"Lovers") + Utils.GetRoleDisplaySpawnMode(CustomRoles.Lovers) + GetString($"LoversInfoLong"));
                AddMsg(sb.ToString(), pc.PlayerId);
            }
        if (msgToSend.Count >= 1)
        {
            var msgTemp = msgToSend.ToList();
            new LateTask(() => { msgTemp.Do(x => Utils.SendMessage(x.Item1, x.Item2, x.Item3 ?? "")); }, 3f, "NotifyOnMeetingStart");
        }

        msgToSend = new();
        CustomRoleManager.AllActiveRoles.Values.Do(x => x.NotifyOnMeetingStart(ref msgToSend));
        msgToSend.Do(x => Logger.Info($"To:{x.Item2} {x.Item3 ?? ""} => {x.Item1}", "NotifyOnMeetingStart"));
        new LateTask(() => { msgToSend.DoIf(x => x.Item1 != null, x => Utils.SendMessage(x.Item1, x.Item2, x.Item3 ?? "")); }, 3f, "NotifyOnMeetingStart");
    }
    public static void Prefix(MeetingHud __instance)
    {
        Logger.Info("------------会议开始------------", "Phase");
        ChatUpdatePatch.DoBlockChat = true;
        GameStates.AlreadyDied |= !Utils.IsAllAlive;
        Main.AllPlayerControls.Do(x => ReportDeadBodyPatch.WaitReport[x.PlayerId].Clear());
        MeetingStates.MeetingCalled = true;
    }
    public static void Postfix(MeetingHud __instance)
    {
        SoundManager.Instance.ChangeAmbienceVolume(0f);
        if (!GameStates.IsModHost) return;
        var myRole = PlayerControl.LocalPlayer.GetRoleClass();

        //提前储存赌怪游戏组件的模板
        GuesserHelper.textTemplate = UnityEngine.Object.Instantiate(__instance.playerStates[0].NameText);
        GuesserHelper.textTemplate.enabled = false;

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
            NotifyOnMeetingStart();
        }

        if (AmongUsClient.Instance.AmHost)
        {
            _ = new LateTask(() =>
            {
                foreach (var pc in Main.AllPlayerControls)
                {
                    pc.RpcSetNameEx(pc.GetRealName(isMeeting: true));
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

        // CreateMeetingButton
        MeetingHudUpdatePatch.ButtonCreated = false;
        if (PlayerControl.LocalPlayer.GetRoleClass() is IMeetingButton meetingButton && meetingButton.ShouldShowButton())
        {
            CreateMeetingButton(__instance, meetingButton);
        }
    }
    public static void CreateMeetingButton(MeetingHud __instance, IMeetingButton meetingButton)
    {
        foreach (var pva in __instance.playerStates)
        {
            var pc = Utils.GetPlayerById(pva.TargetPlayerId);
            if (pc == null || !meetingButton.ShouldShowButtonFor(pc)) continue;
            GameObject template = pva.Buttons.transform.Find("CancelButton").gameObject;
            GameObject targetBox = UnityEngine.Object.Instantiate(template, pva.transform);
            targetBox.name = "Custom Meeting Button";
            targetBox.transform.localPosition = new Vector3(-0.95f, 0.03f, -1.31f);
            SpriteRenderer renderer = targetBox.GetComponent<SpriteRenderer>();
            renderer.sprite = CustomButton.Get(meetingButton.ButtonName);
            PassiveButton button = targetBox.GetComponent<PassiveButton>();
            button.OnClick.RemoveAllListeners();
            button.OnClick.AddListener((Action)(() => {
                if (meetingButton.OnClickButtonLocal(pc))
                {
                    if (AmongUsClient.Instance.AmHost) meetingButton.OnClickButton(pc);
                    else
                    {
                        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.OnClickMeetingButton, SendOption.Reliable, -1);
                        writer.Write(pc.PlayerId);
                        AmongUsClient.Instance.FinishRpcImmediately(writer);
                    }
                }
            }));
        }
        MeetingHudUpdatePatch.ButtonCreated = true;
    }
}
[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Update))]
class MeetingHudUpdatePatch
{
    private static int Count = 0;
    private static void ClearMeetingButton(MeetingHud __instance, bool forceAll = false)
     => __instance.playerStates.ToList().ForEach(x => { if ((forceAll || (!PlayerState.AllPlayerStates.TryGetValue(x.TargetPlayerId, out var ps) || ps.IsDead)) && x.transform.FindChild("Custom Meeting Button") != null) UnityEngine.Object.Destroy(x.transform.FindChild("Custom Meeting Button").gameObject); });
    public static bool ButtonCreated = false;
    public static void Postfix(MeetingHud __instance)
    {
        if (AmongUsClient.Instance.AmHost && Input.GetMouseButtonUp(1) && Input.GetKey(KeyCode.LeftControl))
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

        Count = Count > 20 ? 0 : ++Count;
        if (Count != 0) return;

        //若某玩家死亡则修复会议该玩家状态
        __instance.playerStates.Where(x => (!PlayerState.AllPlayerStates.TryGetValue(x.TargetPlayerId, out var ps) || ps.IsDead) && !x.AmDead).Do(x => x.SetDead(x.DidReport, true));

        //本地玩家并没有会议技能按钮
        if (PlayerControl.LocalPlayer.GetRoleClass() is not IMeetingButton meetingButton) return;

        //投票结束时销毁全部技能按钮
        if (!GameStates.IsVoting && __instance.lastSecond < 1)
        {
            if (GameObject.Find("Custom Meeting Button") != null) ClearMeetingButton(__instance, true);
            return;
        }

        //检查是否应该清除全部按钮
        if (ButtonCreated && !meetingButton.ShouldShowButton())
        {
            ClearMeetingButton(__instance, true);
            ButtonCreated = false;
        }

        //检查是否应该创建按钮
        if (!ButtonCreated && meetingButton.ShouldShowButton())
        {
            MeetingHudStartPatch.CreateMeetingButton(__instance, meetingButton);
        }

        //销毁死亡玩家身上的技能按钮
        ClearMeetingButton(__instance);
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
[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.OnDestroy))]
class MeetingHudOnDestroyPatch
{
    public static void Postfix()
    {
        MeetingStates.FirstMeeting = false;
        Logger.Info("------------会议结束------------", "Phase");
        if (AmongUsClient.Instance.AmHost)
        {
            AntiBlackout.SetIsDead();
            Main.AllPlayerControls.Do(pc => RandomSpawn.CustomNetworkTransformPatch.NumOfTP[pc.PlayerId] = 0);

            Main.LastVotedPlayerInfo = null;
            EAC.MeetingTimes = 0;
        }
    }
}