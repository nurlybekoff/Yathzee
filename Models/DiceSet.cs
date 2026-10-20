namespace Yahtzee.Models;

public class DiceSet
{
    public List<Die> Dice {get; } = new();
    public DiceSet(int count = 5)
    {
        for (int i=0; i<count;i++) Dice.Add(new Die());
    }

    public void Roll()
    {
        foreach (var d in Dice) if (!d.Held) d.Roll();
    }

    public void ResetHeld() {foreach (var d in Dice) d.Reset();}

    public int[] Values => Dice.Select(d => d.Value).ToArray();
}
