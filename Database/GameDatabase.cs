using Microsoft.Data.Sqlite;

namespace Yahtzee.Database;

public record HighScoreEntry(string Name, int Score, DateTime PlayedAt);

public class GameDatabase
{
    private readonly string _conn;

    public GameDatabase(string dbPath = "yahtzee.db")
    {
        _conn = $"Data Source={dbPath}";
        Initialize();
    }

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

    public void RecordGame(IEnumerable<(string Name, int Score)> results)
    {
        using var c = new SqliteConnection(_conn);
        c.Open();
        using var tx = c.BeginTransaction();

        var insertGame = c.CreateCommand();
        insertGame.Transaction = tx;
        insertGame.CommandText = "INSERT INTO Games (PlayedAt) VALUES ($d); SELECT last_insert_rowid();";
        insertGame.Parameters.AddWithValue("$d", DateTime.UtcNow.ToString("o"));
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