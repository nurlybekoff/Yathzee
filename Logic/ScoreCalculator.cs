using Yahtzee.Models;

namespace Yahtzee.Logic;

/// <summary>
/// Pure, stateless scoring rules for all 13 Yahtzee categories, implementing
/// the simplified ruleset from Appendix B of the coursework specification.
/// </summary>
/// <remarks>
/// Each category is evaluated independently against the dice supplied. Empty
/// (zero) dice count as zero for all categories except Chance, and the
/// calculator does not care whose turn it is or which categories are already
/// scored — those are game-level concerns handled by the UI and
/// <see cref="GameManager"/>.
/// </remarks>
public static class ScoreCalculator
{
    /// <summary>
    /// Computes the score for <paramref name="category"/> given the current
    /// dice values.
    /// </summary>
    /// <param name="category">The scoring category to evaluate.</param>
    /// <param name="dice">Five face values, each in the range 1–6.</param>
    /// <returns>
    /// The points this roll would earn in that category. Zero means the roll
    /// does not satisfy the category's requirement (or the category is an
    /// upper-section one and no matching dice are present).
    /// </returns>
    public static int Calculate(Category category, int[] dice)
    {
        // Face-count histogram: counts[v] = number of dice showing face v.
        // Index 0 is unused so that counts[v] directly indexes by face value.
        var counts = new int[7];
        foreach (var d in dice) counts[d]++;

        return category switch
        {
            // ── Upper section: sum only dice matching the face ─────────
            Category.Aces   => SumOf(dice, 1),
            Category.Twos   => SumOf(dice, 2),
            Category.Threes => SumOf(dice, 3),
            Category.Fours  => SumOf(dice, 4),
            Category.Fives  => SumOf(dice, 5),
            Category.Sixes  => SumOf(dice, 6),

            // ── Lower section: "at least N of a kind" scores sum of ALL dice ──
            // Note: at least 3 for ThreeOfAKind and at least 4 for FourOfAKind.
            // Four 6s satisfies both, so both categories score the full sum.
            Category.ThreeOfAKind => counts.Any(c => c >= 3) ? dice.Sum() : 0,
            Category.FourOfAKind  => counts.Any(c => c >= 4) ? dice.Sum() : 0,

            // ── Fixed-score patterns ──────────────────────────────────
            Category.FullHouse     => IsFullHouse(counts)     ? 25 : 0,
            Category.SmallStraight => HasSequence(dice, 4)     ? 30 : 0,
            Category.LargeStraight => HasSequence(dice, 5)     ? 40 : 0,
            Category.Yahtzee       => counts.Any(c => c == 5)  ? 50 : 0,

            // ── Chance: no pattern required, sum everything ───────────
            Category.Chance => dice.Sum(),

            _ => 0
        };
    }

    /// <summary>
    /// Sums the dice matching a single face value. Used by all six upper-section
    /// categories. Two 6s score 12, not 2 — the face value is added once per match.
    /// </summary>
    private static int SumOf(int[] dice, int face) => dice.Where(d => d == face).Sum();

    /// <summary>
    /// Returns <c>true</c> if the histogram contains exactly three of one face
    /// and exactly two of another. A Yahtzee (five of a kind) is <em>not</em> a
    /// full house per the specification — the "exactly 3 / exactly 2" split
    /// excludes it naturally.
    /// </summary>
    private static bool IsFullHouse(int[] counts)
    {
        return counts.Any(c => c == 3) && counts.Any(c => c == 2);
    }

    /// <summary>
    /// Returns <c>true</c> if the dice contain a run of <paramref name="length"/>
    /// consecutive distinct values (anywhere in the roll, not necessarily at
    /// the start). Length 4 = small straight, 5 = large straight.
    /// </summary>
    /// <remarks>
    /// Works on the sorted set of distinct values, so duplicates are ignored:
    /// [1,2,3,3,4] yields a small straight because the distinct values
    /// 1,2,3,4 form a 4-run.
    /// </remarks>
    private static bool HasSequence(int[] dice, int length)
    {
        var distinct = dice.Distinct().OrderBy(x => x).ToArray();
        if (distinct.Length < length) return false;

        for (int i = 0; i <= distinct.Length - length; i++)
        {
            bool seq = true;
            for (int j = 1; j < length; j++)
                if (distinct[i + j] != distinct[i + j - 1] + 1) { seq = false; break; }
            if (seq) return true;
        }
        return false;
    }
}