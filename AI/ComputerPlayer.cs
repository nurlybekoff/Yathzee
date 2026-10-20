using Yahtzee.Logic;
using Yahtzee.Models;

namespace Yahtzee.AI;

/// <summary>
/// Simple "keep the most common face value" AI. Good enough for coursework,
/// but easily swappable for a smarter strategy (e.g., expected-value search).
/// </summary>
public class ComputerPlayer
{
    public void ChooseDiceToHold(DiceSet dice)
    {
        var counts = new int[7];
        foreach (var d in dice.Dice) counts[d.Value]++;

        int bestFace = 1;
        for (int f = 2; f <= 6; f++)
            if (counts[f] > counts[bestFace]) bestFace = f;

        // If we have no pair at all, chase a large straight by keeping 3,4,5,6 or 1,2,3,4.
        if (counts.Max() == 1)
        {
            bool keepLow = dice.Dice.Any(d => d.Value == 1) || dice.Dice.Any(d => d.Value == 2);
            foreach (var d in dice.Dice)
                d.Held = keepLow ? (d.Value <= 4) : (d.Value >= 3);
            return;
        }

        foreach (var d in dice.Dice) d.Held = d.Value == bestFace;
    }

    /// <summary>Pick the category with the highest achievable score.</summary>
    public Category ChooseCategory(int[] dice, ScoreCard card)
    {
        Category best = Category.Chance;
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
        return best;
    }
}