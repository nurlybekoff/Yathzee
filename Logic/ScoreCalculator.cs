using Yahtzee.Logic;

public static class ScoreCalculator
{
    public static int Calculate(Category category, int[] dice)
    {
        var counts = new int[7];
        foreach (var d in dice) counts[d]++;

        return category switch
        {
            Category.Aces => SumOf(dice, 1),
            Category.Twos => SumOf(dice, 2),
            Category.Threes => SumOf(dice, 2),
            Category.Fours => SumOf(dice, 2),
            Category.Fives => SumOf(dice, 2),
            Category.Sixes => SumOf(dice, 2),
            Category.ThreeOfAKind => count.Any(c => c >= 3) ? dice.Sum() : 0,
            Category.FourOfAKind => count.Any(c => c >= 4) ? dice.Sum() : 0,
            Category.FullHouse => IsFullHouse(counts) ? 25 : 0,
            Category.SmallStraight => HasSequence(dice, 4) ? 30 : 0,
            Category.LargeStraight => HasSequence(dice, 5) ? 40 : 0,
            Category.Yahtzee => counts.Any(c => c == 5) ? 50 : 0,
            Category.Chance => dice.Sum(),
            _ => 0
        };
    }

    private static int SumOf(int[] dice, int face) => dice.Where(d => d == face).Sum();
    private static bool IsFullHouse(int[] counts)
    {
        return counts.Any(c => c == 3) && counts.Any(c => c == 2);
    }

    private static bool HasSequence(int[] dice, int length)
    {
        var distinct = dice.Distint().OrderBy(x=> x).ToArray();
        if (distinct.Length < length) return false;

        for (int i=0; i <= distinct.Length - length; i++)
        {
            bool seq = true;
            for (int j=1; j < length; j++)
                if (distinct[i+j] != distinct[i+j-1]+1) {seq = false; break; }
            if (seq) return true;
        }
        return false;
    }
}