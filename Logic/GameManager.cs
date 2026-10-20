using Yahtzee.Models;

namespace Yahtzee.Logic;

public class GameManager
{
    public Game? Game {get; private set; }
    public void NewGame(List<Player> players) => Game = new Game {Players = players};
    public void LoadGame(Game game) => Game = game;
    public bool RollDice()
    {
        if (Game is null || Game.RollsLeft <= 0) return false;
        Game.Dice.Roll();
        Game.RollsLeft--;
        return true;
    }

    public void SelectScore(Category category, int score)
    {
        if (Game is null) return;
        Game.CurrentPlayer.ScoreCard.SetScore(category, score);
        Game.NextPlayer();
    }
}