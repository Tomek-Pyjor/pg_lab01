using Grafika_komputerowa.Interaction;
using Grafika_komputerowa.Model;
using Grafika_komputerowa.Rendering;
using Grafika_komputerowa.UI;

namespace Grafika_komputerowa;

public partial class Form1 : Form
{
    private readonly EditorController _controller = new();
    private readonly SceneRenderer _renderer = new();
    private DrawingCanvas _canvas = null!;
    private Label _status = null!;

    public Form1()
    {
        InitializeComponent();
        BuildUi();
    }

    private void BuildUi()
    {
        Text = "Grafika komputerowa — edytor wielokątów";
        ClientSize = new Size(1100, 720);

        var top = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 40,
            Padding = new Padding(8, 6, 8, 0)
        };

        var rbLib = new RadioButton { Text = "Odcinki: GDI+", AutoSize = true };
        var rbBres = new RadioButton { Text = "Odcinki: Bresenham", AutoSize = true, Checked = true };
        rbLib.CheckedChanged += (_, _) =>
        {
            if (!rbLib.Checked) return;
            _renderer.LineMode = LineDrawMode.Library;
            _canvas.Invalidate();
        };
        rbBres.CheckedChanged += (_, _) =>
        {
            if (!rbBres.Checked) return;
            _renderer.LineMode = LineDrawMode.Bresenham;
            _canvas.Invalidate();
        };
        _renderer.LineMode = LineDrawMode.Bresenham;

        var btnDoc = new Button { Text = "Dokumentacja (F1)", AutoSize = true };
        btnDoc.Click += (_, _) => ShowDocumentation();

        top.Controls.Add(rbBres);
        top.Controls.Add(rbLib);
        top.Controls.Add(btnDoc);

        _status = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 28,
            Padding = new Padding(8, 6, 8, 0),
            Text = _controller.Hint
        };

        _canvas = new DrawingCanvas(_controller, _renderer) { Dock = DockStyle.Fill };
        _canvas.Paint += (_, _) => _status.Text = _controller.Hint;

        Controls.Add(_canvas);
        Controls.Add(top);
        Controls.Add(_status);

        KeyPreview = true;
        KeyDown += Form1_KeyDown;
    }

    private void Form1_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.F1)
        {
            ShowDocumentation();
            e.Handled = true;
            return;
        }

        if (e.KeyCode == Keys.Escape)
        {
            _controller.CancelPending();
            e.Handled = true;
            _canvas.Invalidate();
            _status.Text = _controller.Hint;
        }
    }

    private void ShowDocumentation()
    {
        using var doc = new DocumentationForm();
        doc.ShowDialog(this);
    }
}
