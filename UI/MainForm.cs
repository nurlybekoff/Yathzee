using Yahtzee.AI;
using Yahtzee.Database;
using Yahtzee.Logic;
using Yahtzee.Models;
using Yahtzee.Saving;

namespace Yahtzee.UI;

public partial class MainForm : Form
{
    private readonly GameManager _manager = new();
    private Category? _selectedCategory = null;
    private readonly Dictionary<Category, Panel> _categoryRows = new();
    private readonly GameDatabase _database = new();
    private readonly ComputerPlayer _ai = new();
    private string? _savePath;
    private bool _dirty = false;
    private System.Windows.Forms.Timer? _aiTimer;

    public MainForm()
    {
        InitializeComponent();
        BuildCategoryMenu();  
        UpdateUI();
    }

    // ---------- UI refresh ----------
    private void UpdateScoreboard()
{
    scoreboard.Controls.Clear();
    if (_manager.Game is null) return;

    for (int i = 0; i < _manager.Game.Players.Count; i++)
    {
        var player = _manager.Game.Players[i];
        bool isActive = i == _manager.Game.CurrentPlayerIndex;

        var lbl = new Label
        {
            Text = $"{player.Name}{(player.IsAI ? " (AI)" : "")}:  {player.ScoreCard.Total}",
            AutoSize = true,
            MinimumSize = new Size(140, 34),
            Padding = new Padding(12, 6, 12, 6),
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            BackColor = isActive ? Color.FromArgb(70, 130, 180) : Color.FromArgb(230, 230, 230),
            ForeColor = isActive ? Color.White : Color.Black,
            Margin = new Padding(4, 3, 4, 3),
            BorderStyle = BorderStyle.FixedSingle
        };
        scoreboard.Controls.Add(lbl);
    }
}

    private void UpdateUI()
{
    UpdateScoreboard();
    RefreshCategoryMenu();     // ← replaces the old dgvScores section

    if (_manager.Game is null)
    {
        lblPlayer.Text = "Player: -";
        lblRolls.Text  = "Rolls left: -";
        lblRound.Text  = "Round: -";
        lblStatus.Text = "No game — start a new game.";
        for (int i = 0; i < 5; i++)
        {
            diceButtons[i].Text = "";
            diceButtons[i].Enabled = false;
            diceButtons[i].BackColor = Color.White;
        }
        btnRoll.Enabled = false;
        btnScore.Enabled = false;
        return;
    }

    var g = _manager.Game;
    lblPlayer.Text = $"Player: {g.CurrentPlayer.Name}{(g.CurrentPlayer.IsAI ? " (AI)" : "")}";
    lblRolls.Text  = $"Rolls left: {g.RollsLeft}";
    lblRound.Text  = $"Round: {g.Round}/13";
    lblStatus.Text = g.CurrentPlayer.IsAI
        ? "AI thinking..."
        : "Click a die to hold/unhold. Select a category, then Score.";

    bool interactive = !g.CurrentPlayer.IsAI;
    for (int i = 0; i < 5; i++)
    {
        var die = g.Dice.Dice[i];
        diceButtons[i].Text = die.Value.ToString();
        diceButtons[i].BackColor = die.Held ? Color.FromArgb(144, 222, 176) : Color.White;
        diceButtons[i].Enabled = interactive && g.RollsLeft > 0 && g.RollsLeft < 3;
    }

    btnRoll.Enabled  = interactive && g.RollsLeft > 0;
    btnScore.Enabled = interactive && g.RollsLeft < 3 && _selectedCategory.HasValue;

    // Re-trigger AI as before
    if (g.CurrentPlayer.IsAI && !_isAnimating)
        MaybeRunAI();
}

private bool _isAnimating = false;

private async void AnimateRoll()
{
    if (_isAnimating) return;
    if (_manager.Game is null) return;
    if (!_manager.RollDice()) return;

    _isAnimating = true;
    btnRoll.Enabled = false;

    try
    {
        // Freeze the held-dice state so we only flicker the ones being rolled.
        var heldStatus = _manager.Game.Dice.Dice.Select(d => d.Held).ToArray();

        for (int frame = 0; frame < 8; frame++)
        {
            for (int i = 0; i < 5; i++)
                if (!heldStatus[i])
                    diceButtons[i].Text = Random.Shared.Next(1, 7).ToString();
            await Task.Delay(40);
        }
    }
    finally
    {
        _isAnimating = false;
        _dirty = true;
        UpdateUI();
    }
}

// ═══════════════ Category menu: build / refresh / click ═══════════════

private void BuildCategoryMenu()
{
    categoryPanel.Controls.Clear();
    _categoryRows.Clear();

    AddSectionHeader("UPPER SECTION");
    foreach (var c in new[] { Category.Aces, Category.Twos, Category.Threes,
                              Category.Fours, Category.Fives, Category.Sixes })
        AddCategoryRow(c);

    AddSeparator();

    AddSectionHeader("LOWER SECTION");
    foreach (var c in new[] { Category.ThreeOfAKind, Category.FourOfAKind, Category.FullHouse,
                              Category.SmallStraight, Category.LargeStraight,
                              Category.Yahtzee, Category.Chance })
        AddCategoryRow(c);
}

private void AddSectionHeader(string text)
{
    var lbl = new Label
    {
        Text = text,
        Width = 830,
        Height = 18,
        Font = new Font("Segoe UI", 8, FontStyle.Bold),
        ForeColor = Color.FromArgb(110, 110, 110),
        TextAlign = ContentAlignment.BottomLeft,
        Margin = new Padding(4, 5, 0, 2)
    };
    categoryPanel.Controls.Add(lbl);
}

private void AddSeparator()
{
    var line = new Panel
    {
        Width = 830,
        Height = 1,
        BackColor = Color.FromArgb(190, 190, 190),
        Margin = new Padding(4, 4, 0, 4)
    };
    categoryPanel.Controls.Add(line);
}

private void AddCategoryRow(Category cat)
{
    var row = new Panel
    {
        Width = 830,
        Height = 24,
        BackColor = Color.White,
        Cursor = Cursors.Hand,
        Margin = new Padding(4, 1, 0, 1),
        Tag = cat
    };

    var nameLbl = new Label
    {
        Text = FormatCategoryName(cat),
        Left = 12, Top = 2, Width = 600, Height = 20,
        Font = new Font("Segoe UI", 10),
        AutoSize = false,
        BackColor = Color.Transparent
    };

    var scoreLbl = new Label
    {
        Name = "ScoreLabel",
        Text = "",
        Left = 720, Top = 2, Width = 96, Height = 20,
        Font = new Font("Segoe UI", 10, FontStyle.Bold),
        TextAlign = ContentAlignment.MiddleRight,
        AutoSize = false,
        BackColor = Color.Transparent
    };

    row.Controls.Add(nameLbl);
    row.Controls.Add(scoreLbl);

    EventHandler onClick = (_, _) => SelectCategory(cat);
    row.Click      += onClick;
    nameLbl.Click  += onClick;
    scoreLbl.Click += onClick;

    categoryPanel.Controls.Add(row);
    _categoryRows[cat] = row;
}

private void SelectCategory(Category cat)
{
    if (_manager.Game is null) return;
    if (_manager.Game.CurrentPlayer.IsAI) return;
    if (_manager.Game.RollsLeft >= 3) return;               // must roll first
    if (_manager.Game.CurrentPlayer.ScoreCard.IsUsed(cat)) return;

    _selectedCategory = cat;
    RefreshCategoryMenu();
    btnScore.Enabled = true;
}

private void RefreshCategoryMenu()
{
    var card = _manager.Game?.CurrentPlayer.ScoreCard;
    var dice = _manager.Game?.Dice.Values ?? Array.Empty<int>();
    bool canScore = _manager.Game is not null
                 && !_manager.Game.CurrentPlayer.IsAI
                 && _manager.Game.RollsLeft < 3;

    foreach (var (cat, row) in _categoryRows)
    {
        var nameLbl  = (Label)row.Controls[0];
        var scoreLbl = (Label)row.Controls[1];

        if (card is null || card.IsUsed(cat))
        {
            // Filled (or no game): greyed, shows final score
            scoreLbl.Text = card?.GetScore(cat)?.ToString() ?? "";
            row.BackColor = Color.FromArgb(232, 232, 232);
            nameLbl.ForeColor = Color.Gray;
            scoreLbl.ForeColor = Color.Gray;
            row.Cursor = Cursors.Default;
        }
        else if (_selectedCategory == cat)
        {
            // Selected: blue
            int preview = ScoreCalculator.Calculate(cat, dice);
            scoreLbl.Text = preview.ToString();
            row.BackColor = Color.FromArgb(154, 194, 230);
            nameLbl.ForeColor = Color.Black;
            scoreLbl.ForeColor = Color.Black;
            row.Cursor = canScore ? Cursors.Hand : Cursors.Default;
        }
        else
        {
            // Available: white with black preview
            int preview = ScoreCalculator.Calculate(cat, dice);
            scoreLbl.Text = preview.ToString();
            row.BackColor = Color.White;
            nameLbl.ForeColor = Color.Black;
            scoreLbl.ForeColor = Color.Black;
            row.Cursor = canScore ? Cursors.Hand : Cursors.Default;
        }
    }
}

private static string FormatCategoryName(Category cat) => cat switch
{
    Category.ThreeOfAKind  => "Three of a Kind",
    Category.FourOfAKind   => "Four of a Kind",
    Category.FullHouse     => "Full House",
    Category.SmallStraight => "Small Straight",
    Category.LargeStraight => "Large Straight",
    _                      => cat.ToString()
};

    // ---------- Human actions ----------

    private void OnNewGame(object? sender, EventArgs e)
    {
        using var dlg = new NewGameDialog();
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        _manager.NewGame(dlg.Players);
        _selectedCategory = null;
        _savePath = null;
        _dirty = false;
        UpdateUI();
        MaybeRunAI();
    }

    private void OnRoll(object? sender, EventArgs e)
{
    if (_manager.Game is null || _manager.Game.RollsLeft <= 0) return;
    AnimateRoll();
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
    if (_manager.Game is null) return;
    if (_selectedCategory is not Category cat)
    {
        MessageBox.Show("Click a category on the scorecard first.", "Score");
        return;
    }

    int score = ScoreCalculator.Calculate(cat, _manager.Game.Dice.Values);
    _manager.SelectScore(cat, score);
    _selectedCategory = null;
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

    // Cancel any pending AI step — only one timer may be live at a time.
    _aiTimer?.Stop();
    _aiTimer?.Dispose();

    _aiTimer = new System.Windows.Forms.Timer { Interval = 500 };
    _aiTimer.Tick += (_, _) =>
    {
        _aiTimer?.Stop();
        AIStep();
    };
    _aiTimer.Start();
}

private void AIStep()
{
    if (_manager.Game is null || !_manager.Game.CurrentPlayer.IsAI) return;
    if (_isAnimating) return;
    var g = _manager.Game;

    // Defensive: if the current player's card is full but the game isn't
    // marked over, something has gone out of sync — end the game.
    if (g.CurrentPlayer.ScoreCard.IsComplete)
    {
        g.IsGameOver = true;
        AfterTurn();
        return;
    }

    // Roll branch
    if (g.RollsLeft > 0)
    {
        _ai.ChooseDiceToHold(g.Dice);
        AnimateRoll();
        return;
    }

    // Score branch
    var chosen = _ai.ChooseCategory(g.Dice.Values, g.CurrentPlayer.ScoreCard);
    if (g.CurrentPlayer.ScoreCard.IsUsed(chosen))
    {
        // ChooseCategory returned a category already scored (its default is
        // Chance, which may already be used). Pick any available one.
        var fallback = g.CurrentPlayer.ScoreCard.AvailableCategories.FirstOrDefault();
        if (fallback == default && !g.CurrentPlayer.ScoreCard.AvailableCategories.Any())
        {
            g.IsGameOver = true;
            AfterTurn();
            return;
        }
        chosen = fallback;
    }

    int score = ScoreCalculator.Calculate(chosen, g.Dice.Values);
    _manager.SelectScore(chosen, score);
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
            _selectedCategory = null;
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

    private void OnPlayerHighScores(object? sender, EventArgs e)
{
    string? name = Microsoft.VisualBasic.Interaction.InputBox(
        "Enter the player name whose scores you want to view:",
        "Player High Scores",
        "");
    if (string.IsNullOrWhiteSpace(name)) return;

    var top = _database.GetTopScoresForPlayer(name.Trim(), 5);
    string msg = top.Count == 0
        ? $"No scores recorded for '{name.Trim()}'."
        : string.Join("\n", top.Select((t, i) => $"{i + 1}. {t.Score}  ({t.PlayedAt:d})"));

    MessageBox.Show(this,
        $"Top 5 scores for {name.Trim()}:\n\n{msg}",
        "Player High Scores");
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