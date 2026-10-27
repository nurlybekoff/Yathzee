namespace Yahtzee.Models;

/// <summary>
/// A participant in a game — either a human at the keyboard or a
/// computer-controlled opponent. Each player owns exactly one
/// <see cref="Models.ScoreCard"/> that persists across their turns.
/// </summary>
public class Player
{
    /// <summary>Display name shown in the UI and recorded in the high-score database.</summary>
    public string Name { get; set; }

    /// <summary>When <c>true</c>, the game plays this player's turns automatically via the AI.</summary>
    public bool IsAI { get; set; }

    /// <summary>This player's scorecard. Persists for the full game.</summary>
    public ScoreCard ScoreCard { get; } = new();

    /// <summary>
    /// Creates a player with the given name and AI flag.
    /// </summary>
    /// <param name="name">Display name. May be empty; the UI substitutes a default.</param>
    /// <param name="isAI">If <c>true</c>, the player is computer-controlled.</param>
    public Player(string name, bool isAI = false)
    {
        Name = name;
        IsAI = isAI;
    }
}