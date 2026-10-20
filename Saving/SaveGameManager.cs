using System.Text.Json;
using Yahtzee.Models;

namespace Yahtzee.Saving;

public static class SaveGameManager
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true
    };

    public static void Save(Game game, string path)
    {
        var state = new GameState
        {
            CurrentPlayerIndex = game.CurrentPlayerIndex,
            RollsLeft          = game.RollsLeft,
            Round              = game.Round,
            Players = game.Players.Select(p => new PlayerState
            {
                Name   = p.Name,
                IsAI   = p.IsAI,
                Scores = p.ScoreCard.Scores.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value)
            }).ToList(),
            Dice = game.Dice.Dice
                        .Select(d => new DieState { Value = d.Value, Held = d.Held })
                        .ToList()
        };

        File.WriteAllText(path, JsonSerializer.Serialize(state, Options));
    }

    public static Game Load(string path)
    {
        var state = JsonSerializer.Deserialize<GameState>(File.ReadAllText(path), Options)!;

        var players = state.Players.Select(ps =>
        {
            var p = new Player(ps.Name, ps.IsAI);
            foreach (var kv in ps.Scores)
                if (kv.Value.HasValue)
                    p.ScoreCard.SetScore(Enum.Parse<Category>(kv.Key), kv.Value.Value);
            return p;
        }).ToList();

        var game = new Game
        {
            Players            = players,
            CurrentPlayerIndex = state.CurrentPlayerIndex,
            RollsLeft          = state.RollsLeft,
            Round              = state.Round
        };

        for (int i = 0; i < game.Dice.Dice.Count && i < state.Dice.Count; i++)
        {
            game.Dice.Dice[i].SetValue(state.Dice[i].Value);
            game.Dice.Dice[i].Held = state.Dice[i].Held;
        }
        return game;
    }

    // ---- DTO types ----

    private class GameState
    {
        public int CurrentPlayerIndex { get; set; }
        public int RollsLeft { get; set; }
        public int Round { get; set; }
        public List<PlayerState> Players { get; set; } = new();
        public List<DieState> Dice { get; set; } = new();
    }

    private class PlayerState
    {
        public string Name { get; set; } = "";
        public bool IsAI { get; set; }
        public Dictionary<string, int?> Scores { get; set; } = new();
    }

    private class DieState
    {
        public int Value { get; set; }
        public bool Held { get; set; }
    }
}