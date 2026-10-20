namespace Yahtzee.Models;

public enum Category
{
    Aces, Twos, Threes, Fours, Fives, Sixes, ThreeOfAKind, FourOfAKind, FullHouse, SmallStraight, LargeStraight, Yahtzee, Chance
}

public class ScoreCard
{
    public Dictionary<Category, int?> Scores {get; } = new();

    public ScoreCard()
    {
        foreach (var c in Enum.GetValues<Category>()) Scores[c] = null;
    }   

    public int? GetScore(Category c) => Scores[c];
    public void SetScore(Category c, int value) => Scores[c] = value;
    public bool IsUsed(Category c) => Scores[c].HasValue;
    public bool IsComplete => Scores.Values.All(v => v.HasValue);
    public int Total => Scores.Values.Sum(v => v ?? 0);

    public IEnumerable<Category> AvailableCategories => Scores.Where(kv => !kv.Value.HasValue).Select(kv => kv.key);
}