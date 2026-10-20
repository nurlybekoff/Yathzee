namespace Yahtzee.Models;

public class Player
{
    public string Name {get; set;}
    public bool IsAI {get; set;}
    public ScoreCard ScoreCard {get;} = new();
    public Player(string name, bool isAI = false)
    {
        Name = name;
        IsAI = isAI;
    }
}