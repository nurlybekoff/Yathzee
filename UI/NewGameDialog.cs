using Yahtzee.Models;

namespace Yahtzee.UI;

/// <summary>
/// Modal dialog shown for "New Game", letting the user pick the number of
/// players (1–6), each player's display name, and whether each player is
/// AI-controlled. On OK, exposes the resulting list via <see cref="Players"/>.
/// </summary>
public class NewGameDialog : Form
{
    /// <summary>Populated after <see cref="Form.ShowDialog"/> returns <see cref="DialogResult.OK"/>.</summary>
    public List<Player> Players { get; } = new();

    private readonly NumericUpDown _numPlayers;
    private readonly FlowLayoutPanel _panel;
    private readonly List<TextBox> _nameBoxes = new();
    private readonly List<CheckBox> _aiChecks = new();

    public NewGameDialog()
    {
        Text = "New Game";
        ClientSize = new Size(400, 400);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false; MinimizeBox = false;

        var lbl = new Label { Text = "Number of players:", Top = 12, Left = 10, Width = 120 };
        _numPlayers = new NumericUpDown { Top = 10, Left = 140, Width = 60, Minimum = 1, Maximum = 5, Value = 1 };

        _panel = new FlowLayoutPanel
        {
            Top = 45, Left = 10, Width = 360, Height = 270,
            AutoScroll = true, FlowDirection = FlowDirection.TopDown
        };

        var ok = new Button { Text = "OK", Top = 330, Left = 200, Width = 80, DialogResult = DialogResult.None };
        var cancel = new Button { Text = "Cancel", Top = 330, Left = 290, Width = 80, DialogResult = DialogResult.Cancel };

        Controls.AddRange(new Control[] { lbl, _numPlayers, _panel, ok, cancel });

        _numPlayers.ValueChanged += (_, _) => Rebuild();
        Rebuild();

        ok.Click += (_, _) =>
        {
            Players.Clear();
            for (int i = 0; i < _nameBoxes.Count; i++)
                Players.Add(new Player(
                    string.IsNullOrWhiteSpace(_nameBoxes[i].Text) ? $"Player {i + 1}" : _nameBoxes[i].Text,
                    _aiChecks[i].Checked));
            DialogResult = DialogResult.OK;
        };
        AcceptButton = ok; CancelButton = cancel;
    }

    /// <summary>
    /// Re-creates the per-player row list to match the current value of the
    /// numeric "number of players" control. Called whenever that control changes.
    /// </summary>
    private void Rebuild()
    {
        _panel.Controls.Clear();
        _nameBoxes.Clear();
        _aiChecks.Clear();

        for (int i = 0; i < (int)_numPlayers.Value; i++)
        {
            var row = new FlowLayoutPanel { Width = 340, Height = 32 };
            var tb = new TextBox { Text = $"Player {i + 1}", Width = 200 };
            var cb = new CheckBox { Text = "AI", Width = 60, Top = 3 };
            row.Controls.Add(tb);
            row.Controls.Add(cb);
            _panel.Controls.Add(row);
            _nameBoxes.Add(tb);
            _aiChecks.Add(cb);
        }
    }
}