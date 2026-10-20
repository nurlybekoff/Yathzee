namespace Yahtzee.Models;

public class Die
{
    private static readonly Random Rng = new();

    public int Value {get; private set;} = 1;
    public bool Held {get; set;}
    public void Roll() => Value = Rng.Next(1,7);
    public void SetValue(int v) => Math.Clamp(v,1,6);
    public void Reset() => Held = false;

}