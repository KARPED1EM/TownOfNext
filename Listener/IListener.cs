namespace TownOfHost.Listener;

public interface IListener
{
    void OnPlayerReportBody(PlayerControl reporter, GameData.PlayerInfo target) { }

    bool OnPlayerMurderPlayer(PlayerControl killer, PlayerControl target) { return true; }
}
