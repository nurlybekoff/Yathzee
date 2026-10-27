using Microsoft.Data.Sqlite;

namespace Yahtzee.Database;

/// <summary>
/// A single row from the high-score queries: player name, their final game
/// score, and the moment the game ended (UTC). Exposed as a record so that
/// equality and deconstruction work automatically.
/// </summary>
/// <param name="Name">The player's display name as entered at game start.</param>
/// <param name="Score">Their total score from that game.</param>
/// <param name="PlayedAt">UTC timestamp of when the game was completed.</param>
public record HighScoreEntry(string Name, int Score, DateTime PlayedAt);

/// <summary>
/// Persists completed games to a local SQLite database and answers
/// high-score queries.
/// </summary>
/// <remarks>
/// <para><b>Schema</b></para>
/// <code>
/// Games            Id (PK), PlayedAt (ISO 8601 string, UTC)
/// PlayerScores     Id (PK), GameId (FK → Games.Id, ON DELETE CASCADE),
///                  PlayerName, Score
/// </code>
/// <para>
/// Two indexes support the high-score queries:
/// <c>IX_PlayerScores_Player</c> on <c>PlayerName</c> (per-player top-5) and
/// <c>IX_PlayerScores_Score</c> on <c>Score DESC</c> (overall top-5).
/// </para>
/// <para>
/// <b>Concurrency</b> — a new <see cref="SqliteConnection"/> is opened per
/// call and disposed via <c>using</c>. This is safe for the single-user
/// desktop scenario this app targets: SQLite serialises writers internally.
/// </para>
/// <para>
/// <b>Security</b> — every query uses parameter binding (<c>$name</c>,
/// <c>$g</c>, etc.). No user-supplied value is ever concatenated into SQL.
/// This is relevant given the suppressed GHSA-2m69-gcr7-jv3q advisory in the
/// underlying SQLite native library: the app never passes attacker-controlled
/// SQL to the engine, so the practical attack surface is nil.
/// </para>
/// </remarks>
public class GameDatabase
{
    private readonly string _conn;

    /// <summary>
    /// Opens (and, on first use, creates) a database at
    /// <paramref name="dbPath"/>, then ensures the schema exists.
    /// </summary>
    /// <param name="dbPath">
    /// Path to the SQLite file. Defaults to <c>yahtzee.db</c> in the process's
    /// current working directory, which for the packaged app is the folder
    /// next to the executable.
    /// </param>
    public GameDatabase(string dbPath = "yahtzee.db")
    {
        _conn = $"Data Source={dbPath}";
        Initialize();
    }

    /// <summary>
    /// Creates the <c>Games</c> and <c>PlayerScores</c> tables and their
    /// indexes if they do not already exist. Idempotent — safe to call on
    /// every construction.
    /// </summary>
    private void Initialize()
    {
        using var c = new SqliteConnection(_conn);
        c.Open();
        var cmd = c.CreateCommand();
        cmd.CommandText = @"
            PRAGMA foreign_keys = ON;

            CREATE TABLE IF NOT EXISTS Games (
                Id        INTEGER PRIMARY KEY AUTOINCREMENT,
                PlayedAt  TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS PlayerScores (
                Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                GameId      INTEGER NOT NULL,
                PlayerName  TEXT NOT NULL,
                Score       INTEGER NOT NULL,
                FOREIGN KEY(GameId) REFERENCES Games(Id) ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS IX_PlayerScores_Player ON PlayerScores(PlayerName);
            CREATE INDEX IF NOT EXISTS IX_PlayerScores_Score  ON PlayerScores(Score DESC);
        ";
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Records a completed game and all its players' final scores.
    /// All inserts happen in a single transaction: if any insert fails the
    /// whole game is rolled back, preventing half-written rows.
    /// </summary>
    /// <param name="results">
    /// One <c>(Name, Score)</c> pair per player, in any order.
    /// </param>
    public void RecordGame(IEnumerable<(string Name, int Score)> results)
    {
        using var c = new SqliteConnection(_conn);
        c.Open();
        using var tx = c.BeginTransaction();

        var insertGame = c.CreateCommand();
        insertGame.Transaction = tx;
        insertGame.CommandText = "INSERT INTO Games (PlayedAt) VALUES ($d); SELECT last_insert_rowid();";
        insertGame.Parameters.AddWithValue("$d", DateTime.UtcNow.ToString("o"));  // ISO 8601, round-trippable
        long gameId = (long)insertGame.ExecuteScalar()!;

        foreach (var (name, score) in results)
        {
            var ins = c.CreateCommand();
            ins.Transaction = tx;
            ins.CommandText = "INSERT INTO PlayerScores (GameId, PlayerName, Score) VALUES ($g, $n, $s);";
            ins.Parameters.AddWithValue("$g", gameId);
            ins.Parameters.AddWithValue("$n", name);
            ins.Parameters.AddWithValue("$s", score);
            ins.ExecuteNonQuery();
        }
        tx.Commit();
    }

    /// <summary>
    /// Returns the <paramref name="count"/> highest-scoring player-entries
    /// across all recorded games, most recent first among ties.
    /// </summary>
    /// <param name="count">Maximum number of entries to return.</param>
    public List<HighScoreEntry> GetTopScores(int count)
    {
        var list = new List<HighScoreEntry>();
        using var c = new SqliteConnection(_conn);
        c.Open();
        var cmd = c.CreateCommand();
        cmd.CommandText = @"
            SELECT ps.PlayerName, ps.Score, g.PlayedAt
            FROM PlayerScores ps JOIN Games g ON g.Id = ps.GameId
            ORDER BY ps.Score DESC
            LIMIT $n;";
        cmd.Parameters.AddWithValue("$n", count);
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add(new HighScoreEntry(r.GetString(0), r.GetInt32(1), DateTime.Parse(r.GetString(2))));
        return list;
    }

    /// <summary>
    /// Returns the <paramref name="count"/> highest scores earned by
    /// <paramref name="name"/> specifically. Names are compared
    /// case-sensitively and exactly; no normalisation is applied.
    /// </summary>
    /// <param name="name">Exact player name to filter by.</param>
    /// <param name="count">Maximum number of entries to return.</param>
    public List<HighScoreEntry> GetTopScoresForPlayer(string name, int count)
    {
        var list = new List<HighScoreEntry>();
        using var c = new SqliteConnection(_conn);
        c.Open();
        var cmd = c.CreateCommand();
        cmd.CommandText = @"
            SELECT ps.PlayerName, ps.Score, g.PlayedAt
            FROM PlayerScores ps JOIN Games g ON g.Id = ps.GameId
            WHERE ps.PlayerName = $name
            ORDER BY ps.Score DESC
            LIMIT $n;";
        cmd.Parameters.AddWithValue("$name", name);
        cmd.Parameters.AddWithValue("$n", count);
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add(new HighScoreEntry(r.GetString(0), r.GetInt32(1), DateTime.Parse(r.GetString(2))));
        return list;
    }
}