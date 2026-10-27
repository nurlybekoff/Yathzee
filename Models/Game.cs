namespace Yahtzee.Models;

/// <summary>
/// Represents the full state of an in-progress game: the list of players,
/// the shared dice set, whose turn it is, how many rolls remain in the current
/// turn, and which round the game is on.
/// </summary>
/// <remarks>
/// This class is intentionally a plain data holder — it exposes state but
/// performs only the turn-transition step itself. All orchestration
/// (rolling, scoring, AI scheduling) lives in <c>Yahtzee.Logic.GameManager</c>
/// and the UI layer.
/// </remarks>
public class Game
{
    /// <summary>The players taking part, in turn order.</summary>
    public List<Player> Players { get; set; } = new();

    /// <summary>The shared dice set. Persists across turns, but values are
    /// re-rolled at the start of each player's turn (see <see cref="NextPlayer"/>).</summary>
    public DiceSet Dice { get; } = new();

    /// <summary>Zero-based index into <see cref="Players"/> identifying the active player.</summary>
    public int CurrentPlayerIndex { get; set; } = 0;

    /// <summary>How many rolls the active player has left this turn (3 at the start).</summary>
    public int RollsLeft { get; set; } = 3;

    /// <summary>The current round number, from 1 to 13.</summary>
    public int Round { get; set; } = 1;

    /// <summary>Set to <c>true</c> once every player has filled all 13 categories.</summary>
    public bool IsGameOver { get; set; } = false;

    /// <summary>Convenience accessor for the currently active player.</summary>
    public Player CurrentPlayer => Players[CurrentPlayerIndex];

    /// <summary>
    /// Advances to the next player, resetting per-turn state: rolls remaining
    /// to 3, held flags cleared, and all dice re-rolled so the new player starts
    /// with a fresh board. When the turn wraps back to player 0, the round
    /// counter advances; after round 13 the game is marked complete.
    /// </summary>
    public void NextPlayer()
    {
        CurrentPlayerIndex = (CurrentPlayerIndex + 1) % Players.Count;
        if (CurrentPlayerIndex == 0) Round++;

        RollsLeft = 3;
        Dice.ResetHeld();
        Dice.RollAll();

        if (Round > 13) IsGameOver = true;
    }
}