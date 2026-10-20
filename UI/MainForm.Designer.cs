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

        lblPlayer = new Label { Top = 35, Left = 15, Width = 230, Text = "Player: -", Font = new Font("Segoe UI", 10, FontStyle.Bold) };
        lblRolls  = new Label { Top = 35, Left = 250, Width = 110, Text = "Rolls: -" };
        lblRound  = new Label { Top = 35, Left = 355, Width = 100, Text = "Round: -" };
        lblStatus = new Label { Top = 60, Left = 15, Width = 420, ForeColor = Color.DarkSlateGray };

        diceButtons = new Button[5];
        for (int i = 0; i < 5; i++)
        {
            var b = new Button
            {
                Top = 90, Left = 15 + i * 80, Width = 70, Height = 70,
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                Enabled = false
            };
            b.Click += OnDieClick;
            diceButtons[i] = b;
        }

        btnRoll  = new Button { Text = "Roll", Top = 180, Left = 15,  Width = 180, Height = 40, Enabled = false };
        btnRoll.Click += OnRoll;

        btnScore = new Button { Text = "Score Selected Category", Top = 180, Left = 205, Width = 220, Height = 40, Enabled = false };
        btnScore.Click += OnScore;

        dgvScores = new DataGridView
        {
            Top = 235, Left = 15, Width = 410, Height = 320,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };
        dgvScores.Columns.Add("Category", "Category");
        dgvScores.Columns.Add("Preview", "Preview Score");

        Controls.Add(menuStrip);
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
        ClientSize = new Size(440, 575);
        StartPosition = FormStartPosition.CenterScreen;
    }
}