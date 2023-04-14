using System;
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
        }, 0.9f, "Mafia Kill");
        return true;
    }
}
