using AmongUs.GameOptions;

using TONX.Roles.Core;

namespace TONX.Roles.Neutral;
public sealed class Jester : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Jester),
            player => new Jester(player),
            CustomRoles.Jester,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Neutral,
            50000,
            SetupOptionItem,
            "je|小丑|丑皇",
            "#ec62a5"
        );
    public Jester(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }
    static OptionItem OptionCanUseButton;
    enum OptionName
    {
        JesterCanUseButton
    }
    private static void SetupOptionItem()
    {
        OptionCanUseButton = BooleanOptionItem.Create(RoleInfo, 10, OptionName.JesterCanUseButton, false, false);
    }
    public override void OnExileWrapUp(GameData.PlayerInfo exiled, ref bool DecidedWinner)
    {
        if (!AmongUsClient.Instance.AmHost || Player.PlayerId != exiled.PlayerId) return;

        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Jester);
        CustomWinnerHolder.WinnerIds.Add(exiled.PlayerId);
        DecidedWinner = true;
    }
    public override bool OnCheckReportDeadBody(PlayerControl reporter, GameData.PlayerInfo target)
    {
        if (Is(reporter) && target == null && !OptionCanUseButton.GetBool())
        {
            Logger.Info("因禁止小丑拍灯取消会议", "Jester.OnCheckReportDeadBody");
            return false;
        }
        return true;
    }
}