using Yahtzee.Logic;
using Yahtzee.Models;

namespace Yahtzee.AI;

/// <summary>
/// A simple, stateless computer opponent. Uses a greedy "keep the most common
/// face value" strategy for holding dice, and a greedy "choose the highest
/// currently achievable score" strategy for category selection.
/// </summary>
/// <remarks>
/// <para>
/// The coursework specification explicitly permits a trivial playing strategy,
/// so this AI prioritises clarity and correctness over optimality. It plays
/// legally and completes games, but it does not plan ahead across turns and
/// it ignores the contents of the scorecard when deciding which dice to hold.
/// Both limitations are candidates for future improvement (see the report's
/// "future work" section).
/// </para>
/// <para>
/// The class is stateless — every method takes all needed input as parameters —
/// so a single instance can serve any number of AI players across multiple
/// games. This also makes it trivially unit-testable.
/// </para>
/// </remarks>
public class ComputerPlayer
{
    /// <summary>
    /// Selects which dice to hold before the AI's next roll. Mutates the
    /// <see cref="Die.Held"/> flag on each die in <paramref name="dice"/>.
    /// </summary>
    /// <remarks>
    /// Strategy, in order of priority:
    /// <list type="number">
    ///   <item>
    ///     If there is at least one pair, hold every die showing the most
    ///     frequent face. E.g. [6,6,3,3,1] holds both 6s (and by the greedy
    ///     rule, does not additionally hold the 3s — they could improve).
    ///   </item>
    ///   <item>
    ///     If there are no pairs at all (all five faces distinct), chase a
    ///     large straight by keeping a four-die window: 1,2,3,4 if the roll
    ///     contains a 1 or 2; otherwise 3,4,5,6.
    ///   </item>
    /// </list>
    /// This does not look at the scorecard, so it will hold, say, two 2s even
    /// if Twos has already been scored — an acceptable simplification for a
    /// trivial AI, but noted as future work in the report.
    /// </remarks>
    public void ChooseDiceToHold(DiceSet dice)
    {
        // Face histogram, indexed by face value; index 0 is unused.
        var counts = new int[7];
        foreach (var d in dice.Dice) counts[d.Value]++;

        // Find the most frequent face. Ties resolve to the lowest face
        // because bestFace starts at 1 and only advances on a strict >.
        int bestFace = 1;
        for (int f = 2; f <= 6; f++)
            if (counts[f] > counts[bestFace]) bestFace = f;

        // No pair at all — chase a four-in-a-row instead. The 1-4 window is
        // chosen if the roll contains a low die, else the 3-6 window.
        if (counts.Max() == 1)
        {
            bool keepLow = dice.Dice.Any(d => d.Value == 1) || dice.Dice.Any(d => d.Value == 2);
            foreach (var d in dice.Dice)
                d.Held = keepLow ? (d.Value <= 4) : (d.Value >= 3);
            return;
        }

        // Otherwise hold every die showing the most frequent face.
        foreach (var d in dice.Dice) d.Held = d.Value == bestFace;
    }

    /// <summary>
    /// Chooses which category to score this turn, based on the highest
    /// currently-achievable score across all categories that have not yet
    /// been filled on <paramref name="card"/>.
    /// </summary>
    /// <param name="dice">Current face values (five ints in 1–6).</param>
    /// <param name="card">The AI player's scorecard, used to exclude used categories.</param>
    /// <returns>The category to score into.</returns>
    /// <exception cref="InvalidOperationException">
    /// If <paramref name="card"/> has no available categories. This indicates
    /// a game-state bug (the caller should have detected <see cref="ScoreCard.IsComplete"/>
    /// first); throwing makes the bug visible rather than silently corrupting
    /// an already-scored category.
    /// </exception>
    public Category ChooseCategory(int[] dice, ScoreCard card)
    {
        Category? best = null;
        int bestScore = int.MinValue;

        foreach (var cat in card.AvailableCategories)
        {
            int s = ScoreCalculator.Calculate(cat, dice);
            if (s > bestScore)
            {
                bestScore = s;
                best = cat;
            }
        }

        if (best is null)
            throw new InvalidOperationException(
                "ChooseCategory called with no available categories on the scorecard.");

        return best.Value;
    }
}