namespace Yahtzee.Models;

/// <summary>
/// The 13 scoring categories from a standard Yahtzee scorecard. Upper section
/// (Aces–Sixes) sums matching dice; lower section (ThreeOfAKind–Chance) uses
/// pattern-matching rules. Scoring logic for each category lives in
/// <c>Yahtzee.Logic.ScoreCalculator</c>.
/// </summary>
public enum Category
{
    Aces, Twos, Threes, Fours, Fives, Sixes,
    ThreeOfAKind, FourOfAKind, FullHouse,
    SmallStraight, LargeStraight, Yahtzee, Chance
}

/// <summary>
/// A single player's scorecard. Maps each <see cref="Category"/> to its
/// final score, or <c>null</c> if that category has not yet been used.
/// </summary>
/// <remarks>
/// A category, once scored, cannot be overwritten — the game enforces this
/// by only offering <see cref="AvailableCategories"/> in the UI. The null
/// value is the sentinel for "not yet filled", which lets us distinguish
/// "scored 0" from "not scored".
/// </remarks>
public class ScoreCard
{
    /// <summary>Final score for each category, or <c>null</c> if unfilled.</summary>
    public Dictionary<Category, int?> Scores { get; } = new();

    /// <summary>Creates an empty scorecard with all 13 categories unfilled.</summary>
    public ScoreCard()
    {
        foreach (var c in Enum.GetValues<Category>()) Scores[c] = null;
    }

    /// <summary>Returns the score for a category, or <c>null</c> if unfilled.</summary>
    public int? GetScore(Category c) => Scores[c];

    /// <summary>Records a score for a category. Overwrites any previous value.</summary>
    public void SetScore(Category c, int value) => Scores[c] = value;

    /// <summary>True if the category has already been scored this game.</summary>
    public bool IsUsed(Category c) => Scores[c].HasValue;

    /// <summary>True once all 13 categories have been scored.</summary>
    public bool IsComplete => Scores.Values.All(v => v.HasValue);

    /// <summary>Sum of all filled categories. Unfilled categories contribute 0.</summary>
    public int Total => Scores.Values.Sum(v => v ?? 0);

    /// <summary>Categories that have not yet been scored, in enum declaration order.</summary>
    public IEnumerable<Category> AvailableCategories =>
        Scores.Where(kv => !kv.Value.HasValue).Select(kv => kv.Key);
}