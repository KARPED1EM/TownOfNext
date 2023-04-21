using HarmonyLib;
using Hazel;
using System;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE;

public static class MafiaRevengeManager
{
    public static bool MafiaMsgCheck(PlayerControl pc, string msg)
    {
        if (!AmongUsClient.Instance.AmHost) return false;
        if (!GameStates.IsInGame || pc == null) return false;
        if (!pc.Is(CustomRoles.Mafia)) return false;
        msg = msg.Trim().ToLower();
        if (msg.Length < 3 || msg[..3] != "/rv") return false;
        if (Options.MafiaCanKillNum.GetInt() < 1)
        {
            Utils.SendMessage(GetString("MafiaKillDisable"), pc.PlayerId);
            return true;
        }

        if (!pc.Data.IsDead)
        {
            Utils.SendMessage(GetString("MafiaAliveKill"), pc.PlayerId);
            return true;
        }

        if (msg == "/rv")
        {
            string text = GetString("PlayerIdList");
            foreach (var npc in Main.AllAlivePlayerControls)
                text += "\n" + npc.PlayerId.ToString() + " → (" + npc.GetDisplayRoleName() + ") " + npc.GetRealName();
            Utils.SendMessage(text, pc.PlayerId);
            return true;
        }

        if (Main.MafiaRevenged.ContainsKey(pc.PlayerId))
        {
            if (Main.MafiaRevenged[pc.PlayerId] >= Options.MafiaCanKillNum.GetInt())
            {
                Utils.SendMessage(GetString("MafiaKillMax"), pc.PlayerId);
                return true;
            }
        }
        else
        {
            Main.MafiaRevenged.Add(pc.PlayerId, 0);
        }

        int targetId;
        PlayerControl target;
        try
        {
            targetId = int.Parse(msg.Replace("/rv", String.Empty));
            target = Utils.GetPlayerById(targetId);
        }
        catch
        {
            Utils.SendMessage(GetString("MafiaKillDead"), pc.PlayerId);
            return true;
        }

        if (target == null || target.Data.IsDead)
        {
            Utils.SendMessage(GetString("MafiaKillDead"), pc.PlayerId);
            return true;
        }

        string Name = target.GetRealName();
        Utils.SendMessage(string.Format(GetString("MafiaKillSucceed"), Name), 255, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Mafia), " ★ 特供情报 ★ "));

        new LateTask(() =>
        {
            target.SetRealKiller(pc);
            Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.Revenge;
            target.RpcMurderPlayerV3(target);
            Main.PlayerStates[target.PlayerId].SetDead();
            Main.MafiaRevenged[pc.PlayerId]++;
            foreach (var pc in Main.AllPlayerControls)
                RPC.PlaySoundRPC(pc.PlayerId, Sounds.KillSound);
            ChatUpdatePatch.DoBlockChat = false;
            Utils.NotifyRoles(isForMeeting: true, NoCache: true);
        }, 0.2f, "Mafia Kill");
        return true;
    }

    private static void SendRPC(int playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.MafiaRevenge, SendOption.Reliable, -1);
        writer.Write(playerId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader, PlayerControl pc)
    {
        int PlayerId = reader.ReadInt32();
        MafiaMsgCheck(pc, $"/rv {PlayerId}");
    }

    private static void MafiaOnClick(int index, MeetingHud __instance)
    {
        Logger.Msg($"Click: {index}", "Mafia UI");
        var pc = Utils.GetPlayerById(index);
        if (pc == null || !pc.IsAlive()) return;
        if (AmongUsClient.Instance.AmHost) MafiaMsgCheck(PlayerControl.LocalPlayer, $"/rv {index}");
        else SendRPC(index);
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    class StartMeetingPatch
    {
        public static void Postfix(MeetingHud __instance)
        {
            if (PlayerControl.LocalPlayer.Is(CustomRoles.Mafia) && !PlayerControl.LocalPlayer.IsAlive())
                CreateJudgeButton(__instance);
        }
    }
    public static void CreateJudgeButton(MeetingHud __instance)
    {
        for (int i = 0; i < __instance.playerStates.Length; i++)
        {
            PlayerVoteArea playerVoteArea = __instance.playerStates[i];
            if (Main.PlayerStates[playerVoteArea.TargetPlayerId].IsDead) continue;

            GameObject template = playerVoteArea.Buttons.transform.Find("CancelButton").gameObject;
            GameObject targetBox = UnityEngine.Object.Instantiate(template, playerVoteArea.transform);
            targetBox.name = "ShootButton";
            targetBox.transform.localPosition = new Vector3(-0.95f, 0.03f, -1.3f);
            SpriteRenderer renderer = targetBox.GetComponent<SpriteRenderer>();
            renderer.sprite = Utils.LoadSprite("TOHE.Resources.Images.Skills.TargetIcon.png", 115f);
            PassiveButton button = targetBox.GetComponent<PassiveButton>();
            button.OnClick.RemoveAllListeners();
            int copiedIndex = i;
            button.OnClick.AddListener((Action)(() => MafiaOnClick(copiedIndex, __instance)));
        }
    }
}
