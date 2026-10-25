namespace Grafika_komputerowa.UI;

public sealed class InputDialog : Form
{
    private readonly TextBox _text;

    public string Value => _text.Text;

    public InputDialog(string title, string prompt, string defaultValue)
    {
        Text = title;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(320, 120);

        var lbl = new Label { Text = prompt, Location = new Point(12, 12), AutoSize = true };
        _text = new TextBox { Text = defaultValue, Location = new Point(12, 36), Width = 290 };
        var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(146, 72) };
        var cancel = new Button { Text = "Anuluj", DialogResult = DialogResult.Cancel, Location = new Point(227, 72) };
        AcceptButton = ok;
        CancelButton = cancel;
        Controls.AddRange([lbl, _text, ok, cancel]);
    }
}
