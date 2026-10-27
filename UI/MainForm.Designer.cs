#nullable enable

namespace Yahtzee.UI;

partial class MainForm
{
    private System.ComponentModel.IContainer? components = null;

    private MenuStrip menuStrip = null!;
    private ToolStripMenuItem gameMenu = null!;
    private ToolStripMenuItem newGameItem = null!;
    private ToolStripMenuItem saveItem = null!;
    private ToolStripMenuItem loadItem = null!;
    private ToolStripMenuItem highScoresItem = null!;
    private ToolStripMenuItem exitItem = null!;

    private FlowLayoutPanel scoreboard = null!;
    private Label lblPlayer = null!;
    private Label lblRolls = null!;
    private Label lblRound = null!;
    private Label lblStatus = null!;
    private Button[] diceButtons = null!;
    private Button btnRoll = null!;
    private Button btnScore = null!;
    private DataGridView dgvScores = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        // ── Colors ─────────────────────────────────────────────────
        var bgCream      = Color.FromArgb(250, 248, 240);
        var accentGreen  = Color.FromArgb(144, 222, 176);   // Roll button / held dice
        var accentBlue   = Color.FromArgb(154, 194, 230);   // Score button
        var activePlayer = Color.FromArgb(70, 130, 180);    // Active scoreboard entry
        var passivePl    = Color.FromArgb(230, 230, 230);

        // ── Menu ───────────────────────────────────────────────────
        menuStrip = new MenuStrip();
        gameMenu = new ToolStripMenuItem("Game");
        newGameItem = new ToolStripMenuItem("New Game...");
        saveItem = new ToolStripMenuItem("Save Game...");
        loadItem = new ToolStripMenuItem("Load Game...");
        highScoresItem = new ToolStripMenuItem("High Scores...");
        exitItem = new ToolStripMenuItem("Exit");

        newGameItem.Click += OnNewGame;
        saveItem.Click    += OnSave;
        loadItem.Click    += OnLoad;
        highScoresItem.Click += OnHighScores;
        exitItem.Click    += OnExit;

        gameMenu.DropDownItems.AddRange(new ToolStripItem[]
        {
            newGameItem, new ToolStripSeparator(),
            saveItem, loadItem, new ToolStripSeparator(),
            highScoresItem, new ToolStripSeparator(), exitItem
        });
        menuStrip.Items.Add(gameMenu);

        // ── Scoreboard strip (all players visible at all times) ────
        scoreboard = new FlowLayoutPanel
        {
            Top = 30,
            Left = 15,
            Width = 875,
            Height = 42,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoScroll = true,
            BackColor = bgCream
        };

        // ── Header labels ──────────────────────────────────────────
        lblPlayer = new Label
        {
            Top = 85, Left = 15, Width = 400,
            Text = "Player: -",
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            BackColor = bgCream
        };
        lblRolls  = new Label
        {
            Top = 87, Left = 425, Width = 150,
            Text = "Rolls left: -",
            Font = new Font("Segoe UI", 10),
            BackColor = bgCream
        };
        lblRound  = new Label
        {
            Top = 87, Left = 590, Width = 120,
            Text = "Round: -",
            Font = new Font("Segoe UI", 10),
            BackColor = bgCream
        };
        lblStatus = new Label
        {
            Top = 115, Left = 15, Width = 860,
            Font = new Font("Segoe UI", 9, FontStyle.Italic),
            ForeColor = Color.DarkSlateGray,
            BackColor = bgCream
        };

        // ── Dice buttons (large, clean) ────────────────────────────
        diceButtons = new Button[5];
        for (int i = 0; i < 5; i++)
        {
            var b = new Button
            {
                Top = 150,
                Left = 20 + i * 110,
                Width = 95,
                Height = 95,
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

        // ── Roll / Score buttons ───────────────────────────────────
        btnRoll = new Button
        {
            Text = "🎲  Roll",
            Top = 260, Left = 20, Width = 285, Height = 50,
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
            Top = 260, Left = 320, Width = 285, Height = 50,
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            BackColor = accentBlue,
            FlatStyle = FlatStyle.Flat,
            Enabled = false
        };
        btnScore.FlatAppearance.BorderColor = Color.FromArgb(70, 70, 70);
        btnScore.FlatAppearance.BorderSize = 2;
        btnScore.Click += OnScore;

        // ── Score grid ─────────────────────────────────────────────
        dgvScores = new DataGridView
        {
            Top = 325, Left = 15, Width = 870, Height = 415,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = Color.White,
            GridColor = Color.FromArgb(220, 220, 220),
            BorderStyle = BorderStyle.FixedSingle,
            RowHeadersVisible = false
        };
        dgvScores.Columns.Add("Category", "Category");
        dgvScores.Columns.Add("Preview", "Preview Score");
        dgvScores.Columns[0].FillWeight = 60;
        dgvScores.Columns[1].FillWeight = 40;
        dgvScores.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        dgvScores.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(240, 240, 240);
        dgvScores.EnableHeadersVisualStyles = false;
        dgvScores.DefaultCellStyle.Font = new Font("Segoe UI", 10);
        dgvScores.RowTemplate.Height = 28;

        // ── Compose ────────────────────────────────────────────────
        Controls.Add(menuStrip);
        Controls.Add(scoreboard);
        Controls.Add(lblPlayer);
        Controls.Add(lblRolls);
        Controls.Add(lblRound);
        Controls.Add(lblStatus);
        Controls.Add(btnRoll);
        Controls.Add(btnScore);
        Controls.Add(dgvScores);
        foreach (var b in diceButtons) Controls.Add(b);

        MainMenuStrip = menuStrip;
        Text = "Yahtzee";
        ClientSize = new Size(905, 760);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = bgCream;
        MinimumSize = new Size(905, 760);   // prevent shrinking below what's needed
    }
}