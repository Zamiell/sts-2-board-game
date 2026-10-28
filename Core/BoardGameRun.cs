using BoardGame.Characters;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Runs;

namespace BoardGame.Core;

// A board game run is one where every player is a board game character. Board game acts, rewards, and economy only
// apply to these runs, so STS2 characters keep playing STS2 content.
internal static class BoardGameRun
{
    public static bool IsActive(IRunState? runState) => runState != null && IsBoardGameParty(runState.Players);

    public static bool IsBoardGameParty(IEnumerable<Player> players)
    {
        var any = false;
        foreach (var player in players)
        {
            if (player.Character is not BgCharacter)
            {
                return false;
            }
            any = true;
        }
        return any;
    }
}
