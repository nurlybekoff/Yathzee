#nullable enable

namespace Yahtzee.UI;

/// <summary>
/// Designer-generated layout for <see cref="MainForm"/>. Controls are created
/// in <see cref="InitializeComponent"/>; the accompanying <c>MainForm.cs</c>
/// contains all behaviour. Field naming: <c>lbl*</c> = Label, <c>btn*</c> =
/// Button, <c>*Item</c> = ToolStripMenuItem.
/// </summary>
partial class MainForm
{
    private System.ComponentModel.IContainer? components = null;

    private MenuStrip menuStrip = null!;
    private ToolStripMenuItem gameMenu = null!;
    private ToolStripMenuItem newGameItem = null!;
    private ToolStripMenuItem saveItem = null!;
    private ToolStripMenuItem loadItem = null!;
    private ToolStripMenuItem highScoresItem = null!;
    private ToolStripMenuItem playerHighScoresItem = null!;
    private ToolStripMenuItem exitItem = null!;

    private FlowLayoutPanel scoreboard = null!;
    private Label lblPlayer = null!;
    private Label lblRolls = null!;
    private Label lblRound = null!;
    private Label lblStatus = null!;
    private Button[] diceButtons = null!;
    private Button btnRoll = null!;
    private Button btnScore = null!;
    private FlowLayoutPanel categoryPanel = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        // Palette
        var bgCream     = Color.FromArgb(250, 248, 240);
        var accentGreen = Color.FromArgb(144, 222, 176);
        var accentBlue  = Color.FromArgb(154, 194, 230);

        // ── Menu ───────────────────────────────────────────────────
        menuStrip = new MenuStrip();
        gameMenu = new ToolStripMenuItem("Game");
        newGameItem = new ToolStripMenuItem("New Game...");
        saveItem = new ToolStripMenuItem("Save Game...");
        loadItem = new ToolStripMenuItem("Load Game...");
        highScoresItem = new ToolStripMenuItem("High Scores...");
        playerHighScoresItem = new ToolStripMenuItem("Player High Scores...");
        exitItem = new ToolStripMenuItem("Exit");

        newGameItem.Click          += OnNewGame;
        saveItem.Click             += OnSave;
        loadItem.Click             += OnLoad;
        highScoresItem.Click       += OnHighScores;
        playerHighScoresItem.Click += OnPlayerHighScores;
        exitItem.Click             += OnExit;

        gameMenu.DropDownItems.AddRange(new ToolStripItem[]
        {
            newGameItem, new ToolStripSeparator(),
            saveItem, loadItem, new ToolStripSeparator(),
            highScoresItem, playerHighScoresItem, new ToolStripSeparator(),
            exitItem
        });
        menuStrip.Items.Add(gameMenu);

        // ── Scoreboard strip (all players always visible) ─────────
        scoreboard = new FlowLayoutPanel
        {
            Top = 30, Left = 15, Width = 875, Height = 42,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoScroll = true,
            BackColor = bgCream
        };

        // ── Header labels ─────────────────────────────────────────
        // AutoSize = false + explicit Height prevents the Player label from
        // growing into the status line below.
        lblPlayer = new Label
        {
            Top = 85, Left = 15, Width = 400, Height = 32,
            AutoSize = false,
            Text = "Player: -",
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            BackColor = bgCream
        };
        lblRolls = new Label
        {
            Top = 92, Left = 425, Width = 150, Height = 22,
            AutoSize = false,
            Text = "Rolls left: -",
            Font = new Font("Segoe UI", 10),
            BackColor = bgCream
        };
        lblRound = new Label
        {
            Top = 92, Left = 590, Width = 120, Height = 22,
            AutoSize = false,
            Text = "Round: -",
            Font = new Font("Segoe UI", 10),
            BackColor = bgCream
        };
        lblStatus = new Label
        {
            Top = 128, Left = 15, Width = 860, Height = 20,
            AutoSize = false,
            Font = new Font("Segoe UI", 9, FontStyle.Italic),
            ForeColor = Color.DarkSlateGray,
            BackColor = bgCream
        };

        // ── Dice buttons ──────────────────────────────────────────
        diceButtons = new Button[5];
        for (int i = 0; i < 5; i++)
        {
            var b = new Button
            {
                Top = 160, Left = 20 + i * 110,
                Width = 95, Height = 95,
                Font = new Font("Segoe UI", 32, FontStyle.Bold),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Enabled = false
            };
            b.FlatAppearance.BorderColor = Color.FromArgb(70, 70, 70);
            b.FlatAppearance.BorderSize = 2;
            b.Click += OnDieClick;
            diceButtons[i] = b;
        }

        // ── Roll / Score ──────────────────────────────────────────
        btnRoll = new Button
        {
            Text = "🎲  Roll",
            Top = 270, Left = 20, Width = 285, Height = 50,
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            BackColor = accentGreen,
            FlatStyle = FlatStyle.Flat,
            Enabled = false
        };
        btnRoll.FlatAppearance.BorderColor = Color.FromArgb(70, 70, 70);
        btnRoll.FlatAppearance.BorderSize = 2;
        btnRoll.Click += OnRoll;

        btnScore = new Button
        {
            Text = "✔  Score Selected Category",
            Top = 270, Left = 320, Width = 285, Height = 50,
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            BackColor = accentBlue,
            FlatStyle = FlatStyle.Flat,
            Enabled = false
        };
        btnScore.FlatAppearance.BorderColor = Color.FromArgb(70, 70, 70);
        btnScore.FlatAppearance.BorderSize = 2;
        btnScore.Click += OnScore;

        // ── Category menu ─────────────────────────────────────────
        // Height 435 + 12 px bottom padding ensures 13 rows + 2 headers +
        // 1 separator fit without a scrollbar.
        categoryPanel = new FlowLayoutPanel
        {
            Top = 330, Left = 15, Width = 870, Height = 435,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(8, 6, 8, 12)
        };

        // ── Compose ────────────────────────────────────────────────
        Controls.Add(menuStrip);
        Controls.Add(scoreboard);
        Controls.Add(lblPlayer);
        Controls.Add(lblRolls);
        Controls.Add(lblRound);
        Controls.Add(lblStatus);
        Controls.Add(btnRoll);
        Controls.Add(btnScore);
        Controls.Add(categoryPanel);
        foreach (var b in diceButtons) Controls.Add(b);

        MainMenuStrip = menuStrip;
        Text = "Yahtzee";
        ClientSize = new Size(905, 785);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = bgCream;
        MinimumSize = new Size(905, 785);
    }
}