using Yahtzee.AI;
using Yahtzee.Database;
using Yahtzee.Logic;
using Yahtzee.Models;
using Yahtzee.Saving;

namespace Yahtzee.UI;

public partial class MainForm : Form
{
    private readonly GameManager _manager = new();
    private readonly GameDatabase _database = new();
    private readonly ComputerPlayer _ai = new();
    private string? _savePath;
    private bool _dirty = false;

    public MainForm()
    {
        InitializeComponent();
        UpdateUI();
    }

    // ---------- UI refresh ----------

    private void UpdateUI()
    {
        if (_manager.Game is null)
        {
            lblPlayer.Text = "Player: -";
            lblRolls.Text = "Rolls: -";
            lblRound.Text = "Round: -";
            lblStatus.Text = "No game — start a new game.";
            for (int i = 0; i < 5; i++) { diceButtons[i].Text = ""; diceButtons[i].Enabled = false; diceButtons[i].BackColor = SystemColors.Control; }
            btnRoll.Enabled = false;
            btnScore.Enabled = false;
            dgvScores.Rows.Clear();
            return;
        }

        var g = _manager.Game;
        lblPlayer.Text = $"Player: {g.CurrentPlayer.Name}{(g.CurrentPlayer.IsAI ? " (AI)" : "")}";
        lblRolls.Text  = $"Rolls left: {g.RollsLeft}";
        lblRound.Text  = $"Round: {g.Round}/13";
        lblStatus.Text = g.CurrentPlayer.IsAI ? "AI thinking..." : "Click a die to hold/unhold it.";

        bool interactive = !g.CurrentPlayer.IsAI;
        for (int i = 0; i < 5; i++)
        {
            var die = g.Dice.Dice[i];
            diceButtons[i].Text = die.Value.ToString();
            diceButtons[i].BackColor = die.Held ? Color.LightGreen : SystemColors.Control;
            diceButtons[i].Enabled = interactive && g.RollsLeft > 0 && g.RollsLeft < 3;
        }

        btnRoll.Enabled  = interactive && g.RollsLeft > 0;
        btnScore.Enabled = interactive && g.RollsLeft < 3;

        // Score preview
        var card = g.CurrentPlayer.ScoreCard;
        var dice = g.Dice.Values;
        var selectedCat = dgvScores.SelectedRows.Count > 0
            ? dgvScores.SelectedRows[0].Cells[0].Value?.ToString()
            : null;

        dgvScores.Rows.Clear();
        foreach (var cat in card.AvailableCategories)
        {
            int preview = ScoreCalculator.Calculate(cat, dice);
            int idx = dgvScores.Rows.Add(cat.ToString(), preview);
            if (selectedCat == cat.ToString()) dgvScores.Rows[idx].Selected = true;
        }
    }

    // ---------- Human actions ----------

    private void OnNewGame(object? sender, EventArgs e)
    {
        using var dlg = new NewGameDialog();
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        _manager.NewGame(dlg.Players);
        _savePath = null;
        _dirty = false;
        UpdateUI();
        MaybeRunAI();
    }

    private void OnRoll(object? sender, EventArgs e)
    {
        if (!_manager.RollDice()) return;
        _dirty = true;
        UpdateUI();
    }

    private void OnDieClick(object? sender, EventArgs e)
    {
        if (_manager.Game is null || _manager.Game.RollsLeft >= 3) return;
        var btn = (Button)sender!;
        int idx = Array.IndexOf(diceButtons, btn);
        if (idx < 0) return;
        _manager.Game.Dice.Dice[idx].Held = !_manager.Game.Dice.Dice[idx].Held;
        UpdateUI();
    }

    private void OnScore(object? sender, EventArgs e)
    {
        if (_manager.Game is null || dgvScores.SelectedRows.Count == 0)
        {
            MessageBox.Show("Select a scoring category first.", "Score");
            return;
        }

        var catName = dgvScores.SelectedRows[0].Cells[0].Value?.ToString();
        if (!Enum.TryParse<Category>(catName, out var cat)) return;

        int score = ScoreCalculator.Calculate(cat, _manager.Game.Dice.Values);
        _manager.SelectScore(cat, score);
        AfterTurn();
    }

    private void AfterTurn()
    {
        _dirty = true;
        UpdateUI();

        if (_manager.Game!.IsGameOver) EndGame();
        else MaybeRunAI();
    }

    // ---------- AI ----------

    private void MaybeRunAI()
    {
        if (_manager.Game is null || !_manager.Game.CurrentPlayer.IsAI) return;

        var timer = new System.Windows.Forms.Timer { Interval = 500 };
        timer.Tick += (_, _) => { timer.Stop(); AIStep(); };
        timer.Start();
    }

    private void AIStep()
    {
        if (_manager.Game is null || !_manager.Game.CurrentPlayer.IsAI) return;
        var g = _manager.Game;

        if (g.RollsLeft > 0)
        {
            _ai.ChooseDiceToHold(g.Dice);
            _manager.RollDice();
            _dirty = true;
            UpdateUI();

            if (g.RollsLeft > 0)
            {
                var t = new System.Windows.Forms.Timer { Interval = 500 };
                t.Tick += (_, _) => { t.Stop(); AIStep(); };
                t.Start();
                return;
            }
        }

        var chosen = _ai.ChooseCategory(g.Dice.Values, g.CurrentPlayer.ScoreCard);
        _manager.SelectScore(chosen, ScoreCalculator.Calculate(chosen, g.Dice.Values));
        AfterTurn();
    }

    // ---------- Save / Load ----------

    private void OnSave(object? sender, EventArgs e)
    {
        if (_manager.Game is null) return;
        using var dlg = new SaveFileDialog
        {
            Filter = "Yahtzee save (*.yz)|*.yz",
            FileName = _savePath ?? "game.yz"
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        SaveGameManager.Save(_manager.Game, dlg.FileName);
        _savePath = dlg.FileName;
        _dirty = false;
    }

    private void OnLoad(object? sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog { Filter = "Yahtzee save (*.yz)|*.yz" };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            var g = SaveGameManager.Load(dlg.FileName);
            _manager.LoadGame(g);
            _savePath = dlg.FileName;
            _dirty = false;
            UpdateUI();
            MaybeRunAI();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Failed to load save file: " + ex.Message, "Load Error");
        }
    }

    // ---------- High scores ----------

    private void OnHighScores(object? sender, EventArgs e)
    {
        var top = _database.GetTopScores(5);
        string msg = top.Count == 0
            ? "No games recorded yet."
            : string.Join("\n", top.Select((t, i) =>
                $"{i + 1}. {t.Name}: {t.Score}  ({t.PlayedAt:d})"));
        MessageBox.Show(this, "Top 5 highest scores:\n\n" + msg, "High Scores");
    }

    // ---------- Exit / end ----------

    private void OnExit(object? sender, EventArgs e) => Close();

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (_dirty && _manager.Game is not null)
        {
            var r = MessageBox.Show(this,
                "Save the current game before exiting?",
                "Unsaved Game",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Warning);

            if (r == DialogResult.Cancel) { e.Cancel = true; return; }
            if (r == DialogResult.Yes)    { OnSave(null, EventArgs.Empty); }
        }
        base.OnFormClosing(e);
    }

    private void EndGame()
    {
        var results = _manager.Game!.Players
            .Select(p => (p.Name, p.ScoreCard.Total))
            .OrderByDescending(x => x.Total)
            .ToList();

        _database.RecordGame(results);

        string body = string.Join("\n",
            results.Select((r, i) => $"{i + 1}. {r.Name}: {r.Total}"));
        MessageBox.Show(this, "Game over!\n\n" + body, "Final Results");

        _manager.NewGame(_manager.Game.Players.Select(p => new Player(p.Name, p.IsAI)).ToList());
        _savePath = null;
        _dirty = false;
        UpdateUI();
    }
}