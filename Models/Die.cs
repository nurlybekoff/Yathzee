namespace Yahtzee.Models;

/// <summary>
/// A single six-sided die. Holds its current face value and a
/// <see cref="Held"/> flag indicating whether it should be re-rolled
/// on the next <see cref="DiceSet.Roll"/> call.
/// </summary>
public class Die
{
    // A single shared RNG across all dice. Using one instance avoids the
    // pathological behaviour of new Random() calls in tight succession on
    // .NET Framework (identical seeds); on .NET 6+ this is safe because
    // Random.Shared exists, but a static instance keeps the intent obvious.
    private static readonly Random Rng = new();

    /// <summary>The current face value, in the range 1–6. Defaults to 1.</summary>
    public int Value { get; private set; } = 1;

    /// <summary>
    /// When <c>true</c>, this die is "set aside" and will not be re-rolled
    /// on the next roll. Reset at the start of each new turn.
    /// </summary>
    public bool Held { get; set; }

    /// <summary>Rolls the die, assigning a uniformly random value from 1 to 6.</summary>
    public void Roll() => Value = Rng.Next(1, 7);

    /// <summary>
    /// Forces the die to a specific value, clamped to the legal range 1–6.
    /// Used when restoring a die from a save file.
    /// </summary>
    public void SetValue(int v) => Value = Math.Clamp(v, 1, 6);

    /// <summary>Clears the held flag without changing the value.</summary>
    public void Reset() => Held = false;
}