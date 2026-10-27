namespace Yahtzee.Models;

/// <summary>
/// A set of five six-sided dice that can be rolled together. Individual dice
/// maintain their own <see cref="Die.Held"/> state, so that on the second and
/// third rolls of a turn only the un-held dice change value.
/// </summary>
/// <remarks>
/// The set is the single source of truth for die values during a turn. When
/// the turn passes to the next player, <see cref="Game.NextPlayer"/> calls
/// <see cref="RollAll"/> to give the new player a fresh board — scores are the
/// only state that persists between players.
/// </remarks>
public class DiceSet
{
    /// <summary>The five dice, in left-to-right display order.</summary>
    public List<Die> Dice { get; } = new();

    /// <summary>
    /// Initialises the set with <paramref name="count"/> dice. All dice start
    /// at face value 1 and un-held.
    /// </summary>
    public DiceSet(int count = 5)
    {
        for (int i = 0; i < count; i++) Dice.Add(new Die());
    }

    /// <summary>
    /// Rolls every die that is <em>not</em> currently held. Called on the
    /// second and third rolls of a turn to preserve the player's chosen dice.
    /// </summary>
    public void Roll()
    {
        foreach (var d in Dice) if (!d.Held) d.Roll();
    }

    /// <summary>
    /// Rolls every die regardless of held state. Called when the turn passes
    /// to the next player, so that the new player does not inherit the previous
    /// player's dice.
    /// </summary>
    public void RollAll()
    {
        foreach (var d in Dice) d.Roll();
    }

    /// <summary>
    /// Clears the held flag on every die without changing face values.
    /// </summary>
    public void ResetHeld()
    {
        foreach (var d in Dice) d.Reset();
    }

    /// <summary>The current face values as an array, in left-to-right order.</summary>
    public int[] Values => Dice.Select(d => d.Value).ToArray();
}