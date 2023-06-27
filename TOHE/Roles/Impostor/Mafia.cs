using AmongUs.GameOptions;

using TOHE.Modules;
using TOHE.Roles.Core;
using TOHE.Roles.Core.Interfaces;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;
public sealed class Mafia : RoleBase, IImpostor, IMeetingButton
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Mafia),
            player => new Mafia(player),
            CustomRoles.Mafia,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            2200,
            SetupOptionItem,
            "mf|黑手黨|黑手"
        );
    public Mafia(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        RevengeLimit = OptionRevengeNum.GetInt();
    }

    private static OptionItem OptionRevengeNum;
    enum OptionName
    {
        MafiaCanKillNum,
    }
    public int RevengeLimit = 0;
    private static void SetupOptionItem()
    {
        OptionRevengeNum = IntegerOptionItem.Create(RoleInfo, 10, OptionName.MafiaCanKillNum, new(0, 15, 1), 1, false)
            .SetValueFormat(OptionFormat.Players);
    }
    public override void Add()
    {
        RevengeLimit = OptionRevengeNum.GetInt();
    }
    public bool CanUseKillButton()
    {
        if (PlayerState.AllPlayerStates == null) return false;
        //マフィアを除いた生きているインポスターの人数  Number of Living Impostors excluding mafia
        int livingImpostorsNum = 0;
        foreach (var pc in Main.AllAlivePlayerControls)
        {
            var role = pc.GetCustomRole();
            if (role != CustomRoles.Mafia && role.IsImpostor()) livingImpostorsNum++;
        }

        return livingImpostorsNum <= 0;
    }
    public override bool OnSendMessage(string msg)
    {
        var pc = Player;
        if (!AmongUsClient.Instance.AmHost || !GameStates.IsInGame || pc == null || !pc.Is(CustomRoles.Mafia)) return false;

        msg = msg.Trim().ToLower();
        if (msg.Length < 3 || msg[..3] != "/rv") return false;


        if (msg == "/rv")
        {
            string text = GetString("PlayerIdList");
            foreach (var npc in Main.AllAlivePlayerControls)
                text += "\n" + npc.PlayerId.ToString() + " → (" + Utils.GetTrueRoleName(npc.PlayerId, false) + ") " + npc.GetRealName();
            Utils.SendMessage(text, pc.PlayerId);
            return true;
        }

        int targetId;
        PlayerControl target;
        try
        {
            targetId = int.Parse(msg.Replace("/rv", string.Empty));
            target = Utils.GetPlayerById(targetId);
        }
        catch
        {
            Utils.SendMessage(GetString("MafiaKillDead"), pc.PlayerId);
            return true;
        }

        if (!CanRevenge(target, out var reason))
        {
            Utils.SendMessage(reason, pc.PlayerId);
            return true;
        }

        RevengeLimit--;
        RevengeKill(pc, target);

        return true;
    }
    private static void RevengeKill(PlayerControl killer, PlayerControl target)
    {
        Logger.Info($"{killer.GetNameWithRole()} 复仇了 {target.GetNameWithRole()}", "Mafia");
        CustomSoundsManager.RPCPlayCustomSoundAll("AWP");
        string Name = target.GetRealName();
        new LateTask(() =>
        {
            var state = PlayerState.GetByPlayerId(target.PlayerId);
            state.DeathReason = CustomDeathReason.Revenge;
            target.SetRealKiller(killer);
            if (GameStates.IsMeeting)
            {
                target.RpcSuicideWithAnime();
                //死者检查
                Utils.AfterPlayerDeathTasks(target, true);
                Utils.NotifyRoles(isForMeeting: true, NoCache: true);
            }
            else
            {
                target.RpcMurderPlayer(target);
                state.SetDead();
            }
            new LateTask(() => { Utils.SendMessage(string.Format(GetString("MafiaKillSucceed"), Name), 255, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Mafia), GetString("MafiaRevengeTitle"))); }, 0.6f, "Mafia Kill");
        }, 0.2f, "Mafia Kill");
    }
    private bool CanRevenge(PlayerControl target, out string reason)
    {
        reason = string.Empty;

        if (OptionRevengeNum.GetInt() < 1)
        {
            reason = GetString("MafiaKillDisable");
            return false;
        }
        if (Player.IsAlive())
        {
            reason = GetString("MafiaAliveKill");
            return false;
        }
        if (RevengeLimit <= 0)
        {
            reason = GetString("MafiaKillMax");
            return false;
        }
        if (target == null || target.Data.IsDead)
        {
            reason = GetString("MafiaKillDead");
            return false;
        }
        return true;
    }

    private void MafiaOnClick(PlayerControl target)
    {
        Logger.Msg($"Click: ID {target.GetNameWithRole()}", "Mafia UI");
        if (target == null || !target.IsAlive() || !GameStates.IsVoting) return;

        if (!CanRevenge(target, out var reason))
        {
            PlayerControl.LocalPlayer.ShowPopUp(reason);
            return;
        }

        RevengeLimit--;
        RevengeKill(PlayerControl.LocalPlayer, target);
    }

    public string ButtonName { get; private set; } = "Target";
    public bool ShouldShowButton() => !Player.IsAlive();
    public bool ShouldShowButtonFor(PlayerControl target) => target.IsAlive();
    public void OnClickButton(PlayerControl target) => MafiaOnClick(target);
}