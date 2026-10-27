using System.Text.Json;
using Yahtzee.Models;

namespace Yahtzee.Saving;

/// <summary>
/// Serialises a <see cref="Game"/> to disk and reconstructs it, using a
/// human-readable JSON format.
/// </summary>
/// <remarks>
/// <para>
/// The save format is deliberately a JSON document with a stable, hand-editable
/// schema, so that a save can be inspected in any text editor. It is *not* a
/// binary serialisation: binary formats (BinaryFormatter, protobuf) are opaque
/// to inspection and BinaryFormatter is deprecated as unsafe for untrusted
/// input. JSON also gives us forward compatibility by treating missing fields
/// as their C# defaults.
/// </para>
/// <para>
/// Format (indent expanded for readability):
/// <code>
/// {
///   "CurrentPlayerIndex": 0,          // 0-based index into Players
///   "RollsLeft": 2,                   // 0..3 — rolls remaining this turn
///   "Round": 1,                       // 1..13 — current round
///   "Players": [
///     {
///       "Name": "Alice",
///       "IsAI": false,
///       "Scores": {                   // null = unfilled; integer = final score
///         "Aces": 3,
///         "Twos": null,
///         "Chance": 14
///       }
///     }
///   ],
///   "Dice": [                         // exactly 5 entries
///     { "Value": 6, "Held": true },
///     { "Value": 3, "Held": false }
///   ]
/// }
/// </code>
/// </para>
/// <para>
/// Category names in <c>Scores</c> are the exact <see cref="Category"/> enum
/// member names. All integer fields are required; adding new optional fields
/// is backward-compatible because absent fields deserialise to their C# defaults.
/// </para>
/// </remarks>
public static class SaveGameManager
{
    /// <summary>JSON options used for both read and write — indented output for readability.</summary>
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true
    };

    /// <summary>
    /// Serialises the full state of <paramref name="game"/> to
    /// <paramref name="path"/>, overwriting any existing file.
    /// </summary>
    /// <param name="game">The game to persist. Must not be <c>null</c>.</param>
    /// <param name="path">Destination path; conventionally ends in <c>.yz</c>.</param>
    /// <exception cref="IOException">If the file cannot be written (e.g. locked or path invalid).</exception>
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

    /// <summary>
    /// Reads a save file produced by <see cref="Save"/> and rebuilds a
    /// <see cref="Game"/> object in the exact state it was in when saved.
    /// </summary>
    /// <param name="path">Path to a <c>.yz</c> file.</param>
    /// <returns>A new <see cref="Game"/> instance ready to be resumed.</returns>
    /// <exception cref="IOException">If the file cannot be read.</exception>
    /// <exception cref="JsonException">If the file is not valid save-file JSON.</exception>
    /// <remarks>
    /// Absent or <c>null</c> fields fall back to defaults; this is intentional
    /// for forward-compatibility. Filled categories are restored explicitly;
    /// unfilled categories remain <c>null</c> in the fresh scorecard, matching
    /// their saved state.
    /// </remarks>
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

    // ──────────────────────────────────────────────────────────────────
    // Private DTOs — mirror the on-disk JSON exactly. Kept as private
    // nested classes so the public API never leaks the save schema, and so
    // that the domain models (Game, Player, Die) stay free of serialisation
    // concerns.
    // ──────────────────────────────────────────────────────────────────

    /// <summary>Root of the save-file document. Property names match the JSON keys.</summary>
    private class GameState
    {
        public int CurrentPlayerIndex { get; set; }
        public int RollsLeft { get; set; }
        public int Round { get; set; }
        public List<PlayerState> Players { get; set; } = new();
        public List<DieState> Dice { get; set; } = new();
    }

    /// <summary>Per-player section of a save file.</summary>
    private class PlayerState
    {
        public string Name { get; set; } = "";
        public bool IsAI { get; set; }

        /// <summary>Scores keyed by <see cref="Category"/> enum name; <c>null</c> for unfilled.</summary>
        public Dictionary<string, int?> Scores { get; set; } = new();
    }

    /// <summary>Per-die section of a save file.</summary>
    private class DieState
    {
        public int Value { get; set; }
        public bool Held { get; set; }
    }
}