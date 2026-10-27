using Yahtzee.Models;

namespace Yahtzee.Logic;

/// <summary>
/// Orchestrates the flow of a game: creating/loading games, rolling dice, and
/// recording a chosen category's score. Owns a single <see cref="Models.Game"/>
/// instance, or <c>null</c> when no game is in progress.
/// </summary>
/// <remarks>
/// The GameManager is the UI-facing API for gameplay actions. Each method
/// performs a single atomic operation; the UI is responsible for calling
/// <see cref="UpdateUI"/>-style refreshes afterwards. State-machine logic
/// (whichever player is active, how many rolls remain) is delegated to
/// <see cref="Models.Game"/>.
/// </remarks>
public class GameManager
{
    /// <summary>The game currently in progress, or <c>null</c> if none.</summary>
    public Game? Game { get; private set; }

    /// <summary>Replaces any current game with a freshly created one for the given players.</summary>
    public void NewGame(List<Player> players) => Game = new Game { Players = players };

    /// <summary>Installs an already-constructed game (used when loading a save file).</summary>
    public void LoadGame(Game game) => Game = game;

    /// <summary>
    /// Rolls the current player's un-held dice and decrements the roll counter.
    /// No-op if there is no active game or the player has no rolls left.
    /// </summary>
    /// <returns><c>true</c> if a roll occurred, <c>false</c> otherwise.</returns>
    public bool RollDice()
    {
        if (Game is null || Game.RollsLeft <= 0) return false;
        Game.Dice.Roll();
        Game.RollsLeft--;
        return true;
    }

    /// <summary>
    /// Records <paramref name="score"/> for the current player in
    /// <paramref name="category"/>, then advances to the next player via
    /// <see cref="Game.NextPlayer"/> (which resets rolls and dice for them).
    /// </summary>
    /// <remarks>
    /// The score value is supplied by the caller — usually from
    /// <see cref="ScoreCalculator.Calculate"/> — so that preview values shown
    /// to the user exactly match the values recorded. The manager does not
    /// recompute scores itself.
    /// </remarks>
    public void SelectScore(Category category, int score)
    {
        if (Game is null) return;
        Game.CurrentPlayer.ScoreCard.SetScore(category, score);
        Game.NextPlayer();
    }
}