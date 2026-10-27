namespace Yahtzee.Models;

public class Game
{
    public List<Player> Players {get; set; } = new();
    public DiceSet Dice {get; } = new();
    public int CurrentPlayerIndex {get; set; } = 0;
    public int RollsLeft {get; set; } = 3;
    public int Round {get; set;} = 1;
    public bool IsGameOver {get; set; } = false;
    public Player CurrentPlayer => Players[CurrentPlayerIndex];

    public void NextPlayer()
    {
        CurrentPlayerIndex = (CurrentPlayerIndex + 1) % Players.Count;
        if (CurrentPlayerIndex == 0) Round++;

        RollsLeft = 3;
        Dice.ResetHeld();
        Dice.RollAll();                    // ← new: fresh dice for the next player

        if (Round > 13) IsGameOver = true;
}
}