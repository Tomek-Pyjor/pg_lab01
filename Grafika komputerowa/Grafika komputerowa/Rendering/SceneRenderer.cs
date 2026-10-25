using Grafika_komputerowa.Algorithms;
using Grafika_komputerowa.Geometry;
using Grafika_komputerowa.Interaction;
using Grafika_komputerowa.Model;

namespace Grafika_komputerowa.Rendering;

public sealed class SceneRenderer
{
    public LineDrawMode LineMode { get; set; } = LineDrawMode.Bresenham;

    public void Draw(Graphics g, SceneModel scene, SelectionState sel)
    {
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.Clear(Color.White);

        foreach (var poly in scene.Polygons)
            DrawPolygon(g, poly, sel);

        DrawConstraints(g, scene);
    }

    private void DrawPolygon(Graphics g, Poly poly, SelectionState sel)
    {
        using var edgePen = new Pen(Color.Black, 1.8f);
        using var hullPen = new Pen(Color.FromArgb(180, 80, 80, 80), 1f)
        {
            DashStyle = System.Drawing.Drawing2D.DashStyle.Dash
        };
        using var selPen = new Pen(Color.DodgerBlue, 3f);

        for (var i = 0; i < poly.Count; i++)
        {
            var (a, b) = poly.Endpoints(i);
            var e = poly.E(i);
            var selected = sel.PolygonId == poly.Id && sel.EdgeIndex == i;
            if (selected)
                DrawEdgeHighlight(g, poly, i, a, b, e, selPen);

            DrawEdge(g, poly, i, a, b, e, edgePen);

            if (e.Kind == SegmentKind.Bezier)
            {
                g.DrawLine(hullPen, a.ToPointF(), e.Control1.ToPointF());
                g.DrawLine(hullPen, e.Control1.ToPointF(), e.Control2.ToPointF());
                g.DrawLine(hullPen, e.Control2.ToPointF(), b.ToPointF());
                DrawControl(g, e.Control1);
                DrawControl(g, e.Control2);
            }
            else if (e.Kind == SegmentKind.Arc)
            {
                g.DrawLine(hullPen, a.ToPointF(), b.ToPointF());
            }
        }

        using var font = new Font("Segoe UI", 9f, FontStyle.Bold);
        for (var i = 0; i < poly.Count; i++)
        {
            var v = poly.V(i);
            var isSel = sel.PolygonId == poly.Id && sel.VertexIndex == i;
            FillHandle(g, v.Position, isSel ? Color.DodgerBlue : Color.Black, isSel ? 7 : 5);

            var prev = poly.E(i - 1);
            var next = poly.E(i);
            if (prev.Kind is SegmentKind.Bezier or SegmentKind.Arc ||
                next.Kind is SegmentKind.Bezier or SegmentKind.Arc)
            {
                g.DrawString(v.Continuity.ToString(), font, Brushes.OrangeRed, v.Position.X + 8, v.Position.Y - 18);
            }
        }
    }

    private static void DrawEdgeHighlight(Graphics g, Poly poly, int i, Vec2 a, Vec2 b, Edge e, Pen selPen)
    {
        switch (e.Kind)
        {
            case SegmentKind.Line:
                g.DrawLine(selPen, a.ToPointF(), b.ToPointF());
                break;
            case SegmentKind.Bezier:
                g.DrawLines(selPen, SampleBezier(a, e.Control1, e.Control2, b));
                break;
            case SegmentKind.Arc:
                var arc = Arcs.Sample(poly, i).Select(p => p.ToPointF()).ToArray();
                if (arc.Length >= 2) g.DrawLines(selPen, arc);
                break;
        }
    }

    private static PointF[] SampleBezier(Vec2 p0, Vec2 p1, Vec2 p2, Vec2 p3, int n = 40)
    {
        var pts = new PointF[n + 1];
        for (var i = 0; i <= n; i++)
            pts[i] = Bezier.Eval(p0, p1, p2, p3, i / (float)n).ToPointF();
        return pts;
    }

    private void DrawEdge(Graphics g, Poly poly, int i, Vec2 a, Vec2 b, Edge e, Pen pen)
    {
        switch (e.Kind)
        {
            case SegmentKind.Line:
                DrawLine(g, a, b, pen.Color);
                break;
            case SegmentKind.Bezier:
                using (var brush = new SolidBrush(pen.Color))
                {
                    foreach (var pt in BezierIncremental.Rasterize(a, e.Control1, e.Control2, b))
                        g.FillRectangle(brush, pt.X, pt.Y, 1, 1);
                }
                break;
            case SegmentKind.Arc:
                var pts = Arcs.Sample(poly, i).ToList();
                for (var k = 1; k < pts.Count; k++)
                    DrawLine(g, pts[k - 1], pts[k], pen.Color);
                break;
        }
    }

    private void DrawLine(Graphics g, Vec2 a, Vec2 b, Color color)
    {
        if (LineMode == LineDrawMode.Library)
        {
            using var pen = new Pen(color, 1.8f);
            g.DrawLine(pen, a.ToPointF(), b.ToPointF());
            return;
        }

        using var brush = new SolidBrush(color);
        foreach (var pt in Bresenham.Line((int)a.X, (int)a.Y, (int)b.X, (int)b.Y))
            g.FillRectangle(brush, pt.X, pt.Y, 1, 1);
    }

    private static void FillHandle(Graphics g, Vec2 p, Color c, float r = 5)
    {
        using var brush = new SolidBrush(c);
        g.FillEllipse(brush, p.X - r, p.Y - r, r * 2, r * 2);
    }

    private static void DrawControl(Graphics g, Vec2 p)
    {
        const float r = 6f;
        using var fill = new SolidBrush(Color.OrangeRed);
        using var ring = new Pen(Color.White, 1.5f);
        g.FillEllipse(fill, p.X - r, p.Y - r, r * 2, r * 2);
        g.DrawEllipse(ring, p.X - r, p.Y - r, r * 2, r * 2);
    }

    private static void DrawConstraints(Graphics g, SceneModel scene)
    {
        using var font = new Font("Segoe UI", 9f, FontStyle.Bold);
        using var arrow = new Pen(Color.OrangeRed, 2f)
        {
            EndCap = System.Drawing.Drawing2D.LineCap.ArrowAnchor,
            StartCap = System.Drawing.Drawing2D.LineCap.ArrowAnchor
        };

        foreach (var c in scene.Constraints)
        {
            switch (c)
            {
                case LengthConstraint len when scene.Get(len.PolygonId) is { } p:
                    var midL = ConstraintIcons.Mid(p, len.EdgeIndex);
                    g.DrawString($"{len.Length:F0}", font, Brushes.OrangeRed, midL.X + 6, midL.Y - 14);
                    break;
                case PairConstraint { Kind: ConstraintKind.Perpendicular } perp:
                    DrawPerpMark(g, scene, perp.PolygonA, perp.EdgeA, arrow);
                    DrawPerpMark(g, scene, perp.PolygonB, perp.EdgeB, arrow);
                    break;
            }
        }
    }

    private static void DrawPerpMark(Graphics g, SceneModel scene, int polyId, int edge, Pen arrow)
    {
        var p = scene.Get(polyId);
        if (p == null || edge < 0) return;
        var (a, b) = p.Endpoints(edge);
        var mid = ConstraintIcons.Mid(p, edge);
        var dir = (b - a).Normalized();
        var n = dir.Perp();
        var p0 = mid - n * 14;
        var p1 = mid + n * 14;
        g.DrawLine(arrow, p0.ToPointF(), p1.ToPointF());
    }
}
