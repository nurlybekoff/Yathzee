using Yahtzee.AI;
using Yahtzee.Database;
using Yahtzee.Logic;
using Yahtzee.Models;
using Yahtzee.Saving;

namespace Yahtzee.UI;

/// <summary>
/// Primary application window. Hosts the dice, roll/score controls, category
/// menu, player scoreboard strip, and the main menu (new game, save/load, high
/// scores, exit). This is the only class that mutates UI state in response to
/// game events.
/// </summary>
/// <remarks>
/// <para>
/// The form operates in one of two modes: (a) no active game — a game must be
/// started or loaded; (b) an active game — human turns are driven by clicks,
/// AI turns by a single live <see cref="System.Windows.Forms.Timer"/> created
/// on-demand in <see cref="MaybeRunAI"/>.
/// </para>
/// <para>
/// Game state lives in <see cref="GameManager"/>; this class reads it via
/// <see cref="GameManager.Game"/> and calls mutator methods on the manager.
/// All UI refresh is funnelled through <see cref="UpdateUI"/>, which is safe
/// to call at any time.
/// </para>
/// </remarks>
public partial class MainForm : Form
{
    private readonly GameManager _manager = new();
    private readonly GameDatabase _database = new();
    private readonly ComputerPlayer _ai = new();

    /// <summary>Currently highlighted category (null = none selected).</summary>
    private Category? _selectedCategory = null;

    /// <summary>Map from category to the UI row that represents it.</summary>
    private readonly Dictionary<Category, Panel> _categoryRows = new();

    /// <summary>Path of the last save file, offered as the default on re-save.</summary>
    private string? _savePath;

    /// <summary>Set when state has changed since the last save; drives the exit prompt.</summary>
    private bool _dirty = false;

    /// <summary>The single live AI step timer; only one may exist at a time.</summary>
    private System.Windows.Forms.Timer? _aiTimer;

    /// <summary>Guard preventing re-entrant roll animations.</summary>
    private bool _isAnimating = false;

    public MainForm()
    {
        InitializeComponent();
        BuildCategoryMenu();
        UpdateUI();
    }

    // ────────────────────────────────────────────────────────────────
    // UI refresh
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Rebuilds the player scoreboard strip — one label per player showing
    /// name, AI marker, and running total. The active player's label is
    /// highlighted in blue.
    /// </summary>
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

    /// <summary>
    /// Single source of truth for repainting the UI. Reads the current game
    /// state and updates every affected control: scoreboard, header labels,
    /// dice buttons, roll/score enablement, and the category menu preview.
    /// Also re-arms the AI step timer if it is currently the AI's turn.
    /// </summary>
    private void UpdateUI()
    {
        UpdateScoreboard();
        RefreshCategoryMenu();

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

        // Re-trigger AI whenever it is their turn and no animation is running.
        // Not gated on RollsLeft > 0, because the AI still has to score after
        // its final roll — AIStep handles the "no rolls left → score" branch.
        if (g.CurrentPlayer.IsAI && !_isAnimating)
            MaybeRunAI();
    }

    /// <summary>
    /// Performs a roll with a short visual flicker of the affected dice, then
    /// commits the real roll and refreshes the UI. Flicker skips held dice so
    /// the visual matches what will actually change.
    /// </summary>
    /// <remarks>
    /// <c>async void</c> is the accepted pattern for UI event handlers that
    /// need to <c>await</c>; there is no caller to propagate an exception to,
    /// and the <c>finally</c> block guarantees the animation flag is cleared.
    /// </remarks>
    private async void AnimateRoll()
    {
        if (_isAnimating) return;
        if (_manager.Game is null) return;
        if (!_manager.RollDice()) return;

        _isAnimating = true;
        btnRoll.Enabled = false;

        try
        {
            // Freeze the held-state before animating so the flicker only
            // touches dice that will actually be re-rolled.
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

    // ────────────────────────────────────────────────────────────────
    // Category menu
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds the two-section category menu (upper + lower), with separator
    /// lines between them. Called once at startup; content does not change
    /// during a game — state is updated in-place by <see cref="RefreshCategoryMenu"/>.
    /// </summary>
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

    /// <summary>Adds a small grey section caption ("UPPER SECTION", etc.).</summary>
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

    /// <summary>Adds a thin horizontal rule between the two scorecard sections.</summary>
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

    /// <summary>
    /// Creates one category row: a clickable panel containing the category
    /// name (left) and a live score preview (right). Click on any part of the
    /// row selects the category.
    /// </summary>
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

    /// <summary>
    /// Handles a click on a category row. Ignores the click if a game is not
    /// running, if it is the AI's turn, if the player has not rolled yet, or
    /// if the category is already filled — none of those are legal selections.
    /// </summary>
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

    /// <summary>
    /// Repaints every category row according to current game state:
    /// <list type="bullet">
    ///   <item>Filled rows: greyed, showing the committed score.</item>
    ///   <item>Selected row: blue, showing live preview.</item>
    ///   <item>Available rows: white, showing live preview.</item>
    /// </list>
    /// </summary>
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
                scoreLbl.Text = card?.GetScore(cat)?.ToString() ?? "";
                row.BackColor = Color.FromArgb(232, 232, 232);
                nameLbl.ForeColor = Color.Gray;
                scoreLbl.ForeColor = Color.Gray;
                row.Cursor = Cursors.Default;
            }
            else if (_selectedCategory == cat)
            {
                int preview = ScoreCalculator.Calculate(cat, dice);
                scoreLbl.Text = preview.ToString();
                row.BackColor = Color.FromArgb(154, 194, 230);
                nameLbl.ForeColor = Color.Black;
                scoreLbl.ForeColor = Color.Black;
                row.Cursor = canScore ? Cursors.Hand : Cursors.Default;
            }
            else
            {
                int preview = ScoreCalculator.Calculate(cat, dice);
                scoreLbl.Text = preview.ToString();
                row.BackColor = Color.White;
                nameLbl.ForeColor = Color.Black;
                scoreLbl.ForeColor = Color.Black;
                row.Cursor = canScore ? Cursors.Hand : Cursors.Default;
            }
        }
    }

    /// <summary>Formats an enum category name for display (adds spaces for compound names).</summary>
    private static string FormatCategoryName(Category cat) => cat switch
    {
        Category.ThreeOfAKind  => "Three of a Kind",
        Category.FourOfAKind   => "Four of a Kind",
        Category.FullHouse     => "Full House",
        Category.SmallStraight => "Small Straight",
        Category.LargeStraight => "Large Straight",
        _                      => cat.ToString()
    };

    // ────────────────────────────────────────────────────────────────
    // Human actions (event handlers — behaviour is by name)
    // ────────────────────────────────────────────────────────────────

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

    /// <summary>
    /// Shared post-turn step: marks state dirty, refreshes the UI, and either
    /// ends the game (if complete) or hands control to the AI (if it is now
    /// the AI's turn).
    /// </summary>
    private void AfterTurn()
    {
        _dirty = true;
        UpdateUI();

        if (_manager.Game!.IsGameOver) EndGame();
        else MaybeRunAI();
    }

    // ────────────────────────────────────────────────────────────────
    // AI
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Arms the AI step timer, replacing any existing timer. Only one timer
    /// may be live at a time — calling this while a step is pending cancels
    /// the pending step and schedules a new one.
    /// </summary>
    private void MaybeRunAI()
    {
        if (_manager.Game is null || !_manager.Game.CurrentPlayer.IsAI) return;

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

    /// <summary>
    /// Executes one atomic step of the AI's turn: either roll (and re-arm the
    /// timer via <see cref="AnimateRoll"/> → <see cref="UpdateUI"/>), or score
    /// (and hand control to the human). Guards against being called mid-
    /// animation and against the impossible state where a full scorecard
    /// coexists with a not-yet-ended game.
    /// </summary>
    private void AIStep()
    {
        if (_manager.Game is null || !_manager.Game.CurrentPlayer.IsAI) return;
        if (_isAnimating) return;
        var g = _manager.Game;

        // Defensive: a full card with the game not marked over indicates state
        // has drifted (e.g. after loading a malformed save). End the game.
        if (g.CurrentPlayer.ScoreCard.IsComplete)
        {
            g.IsGameOver = true;
            AfterTurn();
            return;
        }

        // Roll branch — animation re-arms the timer via UpdateUI.
        if (g.RollsLeft > 0)
        {
            _ai.ChooseDiceToHold(g.Dice);
            AnimateRoll();
            return;
        }

        // Score branch.
        // ChooseCategory throws if the card has no available categories; the
        // IsComplete guard above prevents that path from ever being reached.
        var chosen = _ai.ChooseCategory(g.Dice.Values, g.CurrentPlayer.ScoreCard);
        int score = ScoreCalculator.Calculate(chosen, g.Dice.Values);
        _manager.SelectScore(chosen, score);
        AfterTurn();
    }

    // ────────────────────────────────────────────────────────────────
    // Save / Load
    // ────────────────────────────────────────────────────────────────

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

    // ────────────────────────────────────────────────────────────────
    // High scores
    // ────────────────────────────────────────────────────────────────

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

    // ────────────────────────────────────────────────────────────────
    // Exit / end-of-game
    // ────────────────────────────────────────────────────────────────

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

    /// <summary>
    /// Called when the game ends. Records the game to the database, shows the
    /// results ordered by score, then starts a fresh game with the same players.
    /// </summary>
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