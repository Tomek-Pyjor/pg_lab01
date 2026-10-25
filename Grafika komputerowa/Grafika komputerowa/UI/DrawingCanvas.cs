using Grafika_komputerowa.Interaction;
using Grafika_komputerowa.Model;
using Grafika_komputerowa.Rendering;

namespace Grafika_komputerowa.UI;

public sealed class DrawingCanvas : Panel
{
    public EditorController Controller { get; }
    public SceneRenderer Renderer { get; }

    public DrawingCanvas(EditorController controller, SceneRenderer renderer)
    {
        Controller = controller;
        Renderer = renderer;
        DoubleBuffered = true;
        TabStop = true;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Renderer.Draw(e.Graphics, Controller.Scene, Controller.Selection);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        Focus();
        var world = ScreenToWorld(e.Location);
        var hit = Controller.Pick(world);

        if (e.Button == MouseButtons.Right)
        {
            Controller.Select(hit);
            Invalidate();
            ShowContextMenu(hit, e.Location);
            return;
        }

        if (e.Button == MouseButtons.Left)
        {
            if (Controller.TryFinishPending(hit))
            {
                Invalidate();
                return;
            }

            if (hit.Kind == HitKind.ConstraintIcon && hit.ConstraintId is { } id)
            {
                Controller.RemoveConstraint(id);
                Invalidate();
                return;
            }

            Controller.Select(hit);
            Controller.BeginDrag(hit, world);
            Invalidate();
        }

        base.OnMouseDown(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            Controller.OnMouseMove(ScreenToWorld(e.Location));
            Invalidate();
        }
        base.OnMouseMove(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        Controller.OnMouseUp();
        base.OnMouseUp(e);
    }

    private void ShowContextMenu(Hit hit, Point location)
    {
        var menu = new ContextMenuStrip();

        switch (hit.Kind)
        {
            case HitKind.Edge:
                AddEdgeMenu(menu);
                break;
            case HitKind.Vertex:
                AddVertexMenu(menu);
                break;
            case HitKind.ConstraintIcon when hit.ConstraintId is { } id:
                menu.Items.Add("Usuń to ograniczenie", null, (_, _) =>
                {
                    Controller.RemoveConstraint(id);
                    Invalidate();
                });
                break;
            default:
                return;
        }

        if (menu.Items.Count == 0) return;
        menu.Show(this, location);
    }

    private void AddEdgeMenu(ContextMenuStrip menu)
    {
        var kind = Controller.SelectedEdgeKind();
        menu.Items.Add("Dodaj wierzchołek w środku", null, (_, _) =>
        {
            Controller.InsertVertexOnSelectedEdge();
            Invalidate();
        });
        menu.Items.Add(new ToolStripSeparator());
        Checked(menu, "Odcinek", kind == SegmentKind.Line, () => Controller.SetSelectedEdgeKind(SegmentKind.Line));
        Checked(menu, "Segment Béziera", kind == SegmentKind.Bezier, () => Controller.SetSelectedEdgeKind(SegmentKind.Bezier));
        Checked(menu, "Łuk okręgu", kind == SegmentKind.Arc, () => Controller.SetSelectedEdgeKind(SegmentKind.Arc));
        if (kind == SegmentKind.Arc)
            menu.Items.Add("Odwróć stronę łuku", null, (_, _) => { Controller.FlipArcSide(); Invalidate(); });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Zadana długość…", null, (_, _) => PromptLength());
        menu.Items.Add("Prostopadłość z inną krawędzią…", null, (_, _) =>
        {
            Controller.BeginPickPerpendicular();
            Invalidate();
        });
        menu.Items.Add("Usuń ograniczenia tej krawędzi", null, (_, _) =>
        {
            Controller.RemoveConstraintAtSelectedEdge();
            Invalidate();
        });
    }

    private void AddVertexMenu(ContextMenuStrip menu)
    {
        menu.Items.Add("Usuń wierzchołek", null, (_, _) =>
        {
            Controller.DeleteSelectedVertex();
            Invalidate();
        });
        if (!Controller.SelectedVertexTouchesCurve()) return;
        menu.Items.Add(new ToolStripSeparator());
        var arc = Controller.SelectedVertexTouchesArc();
        menu.Items.Add("Ciągłość G0 (położenie)", null, (_, _) =>
        {
            Controller.SetSelectedVertexContinuity(Continuity.G0);
            Invalidate();
        });
        menu.Items.Add("Ciągłość G1 (kierunek stycznej)", null, (_, _) =>
        {
            Controller.SetSelectedVertexContinuity(Continuity.G1);
            Invalidate();
        });
        if (!arc)
            menu.Items.Add("Ciągłość C1 (wektor styczny)", null, (_, _) =>
            {
                Controller.SetSelectedVertexContinuity(Continuity.C1);
                Invalidate();
            });
    }

    private void Checked(ContextMenuStrip menu, string text, bool isChecked, Action action)
    {
        var item = new ToolStripMenuItem(text) { Checked = isChecked };
        item.Click += (_, _) => { action(); Invalidate(); };
        menu.Items.Add(item);
    }

    private void PromptLength()
    {
        var current = Controller.GetSelectedEdgeLength();
        if (current <= 0) return;
        using var dlg = new InputDialog("Zadana długość krawędzi", "Długość:", current.ToString("F1"));
        if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
        if (float.TryParse(dlg.Value.Replace(',', '.'), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var len) && len > 0)
        {
            Controller.AddFixedLengthConstraint(len);
            Invalidate();
        }
    }

    private static Vec2 ScreenToWorld(Point p) => new(p.X, p.Y);
}
