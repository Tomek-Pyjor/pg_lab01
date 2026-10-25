using Grafika_komputerowa.Geometry;
using Grafika_komputerowa.Model;
using Grafika_komputerowa.Scene;

namespace Grafika_komputerowa.Interaction;

public sealed class SelectionState
{
    public int PolygonId { get; set; } = -1;
    public int VertexIndex { get; set; } = -1;
    public int EdgeIndex { get; set; } = -1;
    public int SecondEdgePolygonId { get; set; } = -1;
    public int SecondEdgeIndex { get; set; } = -1;

    public void Clear()
    {
        PolygonId = VertexIndex = EdgeIndex = -1;
        SecondEdgePolygonId = SecondEdgeIndex = -1;
    }
}

public enum PendingAction
{
    None,
    PickPerpendicularEdge
}

public sealed class EditorController
{
    public SceneModel Scene { get; } = new();
    public SelectionState Selection { get; } = new();
    public PendingAction Pending { get; private set; }
    public string Hint { get; private set; } =
        "LPM — przeciąganie. PPM na krawędzi / wierzchołku — menu edycji. F1 — dokumentacja.";

    private Hit _drag;
    private Vec2 _dragOffset;

    public EditorController() => DemoScene.Build(Scene);

    public Hit Pick(Vec2 world) => HitTest.Pick(Scene, world);

    public void Select(Hit hit)
    {
        switch (hit.Kind)
        {
            case HitKind.Vertex:
                Selection.PolygonId = hit.PolygonId;
                Selection.VertexIndex = hit.Index;
                Selection.EdgeIndex = -1;
                break;
            case HitKind.Control1:
            case HitKind.Control2:
            case HitKind.Edge:
                Selection.PolygonId = hit.PolygonId;
                Selection.EdgeIndex = hit.Index;
                Selection.VertexIndex = -1;
                break;
            case HitKind.Polygon:
                Selection.Clear();
                Selection.PolygonId = hit.PolygonId;
                break;
            default:
                Selection.Clear();
                break;
        }

        foreach (var p in Scene.Polygons)
            p.IsSelected = p.Id == hit.PolygonId;
    }

    public bool TryFinishPending(Hit hit)
    {
        if (Pending != PendingAction.PickPerpendicularEdge) return false;
        if (hit.Kind != HitKind.Edge)
        {
            Hint = "Kliknij LPM drugą krawędź (może być z innego wielokąta). Esc anuluje.";
            return true;
        }

        Selection.SecondEdgePolygonId = hit.PolygonId;
        Selection.SecondEdgeIndex = hit.Index;
        Pending = PendingAction.None;
        AddPerpendicularConstraint();
        return true;
    }

    public void CancelPending()
    {
        Pending = PendingAction.None;
        Hint = "Anulowano.";
    }

    public void BeginDrag(Hit hit, Vec2 world)
    {
        _drag = hit;
        switch (hit.Kind)
        {
            case HitKind.Vertex:
                _dragOffset = Scene.Get(hit.PolygonId)!.P(hit.Index) - world;
                Hint = "Przesuwanie wierzchołka.";
                break;
            case HitKind.Control1:
            case HitKind.Control2:
                Hint = "Przesuwanie punktu kontrolnego (wierzchołki tego segmentu zostają).";
                break;
            case HitKind.Polygon:
                _dragOffset = world;
                Hint = "Przesuwanie całego wielokąta.";
                break;
            default:
                _drag = new Hit(HitKind.None);
                break;
        }
    }

    public void OnMouseMove(Vec2 world)
    {
        if (_drag.Kind == HitKind.None) return;
        var poly = Scene.Get(_drag.PolygonId);
        if (poly == null) return;

        switch (_drag.Kind)
        {
            case HitKind.Vertex:
                poly.V(_drag.Index).Position = world + _dragOffset;
                ContinuitySolver.AfterVertexMoved(poly, _drag.Index);
                ConstraintSolver.Solve(Scene, poly.Id, _drag.Index, true);
                break;
            case HitKind.Control1:
                poly.E(_drag.Index).Control1 = world;
                ContinuitySolver.AfterControlMoved(poly, _drag.Index, true);
                ConstraintSolver.Solve(Scene, poly.Id, _drag.Index, false);
                break;
            case HitKind.Control2:
                poly.E(_drag.Index).Control2 = world;
                ContinuitySolver.AfterControlMoved(poly, _drag.Index, false);
                ConstraintSolver.Solve(Scene, poly.Id, _drag.Index, false);
                break;
            case HitKind.Polygon:
                poly.Translate(world - _dragOffset);
                _dragOffset = world;
                ConstraintSolver.Solve(Scene, poly.Id, -1, false);
                break;
        }
    }

    public void OnMouseUp() => _drag = new Hit(HitKind.None);

    public void DeleteSelectedVertex()
    {
        if (Selection.VertexIndex < 0 || Selection.PolygonId < 0) return;
        var poly = Scene.Get(Selection.PolygonId);
        if (poly == null) return;
        var oldCount = poly.Count;
        var vi = poly.Wrap(Selection.VertexIndex);
        var prev = poly.Wrap(vi - 1);
        Scene.RemoveConstraintsOf(poly.Id, prev);
        Scene.RemoveConstraintsOf(poly.Id, vi);
        if (!poly.RemoveVertex(vi))
        {
            Hint = "Wielokąt musi mieć co najmniej 3 wierzchołki.";
            return;
        }
        Scene.NotifyEdgeRemoved(poly.Id, vi, oldCount);
        Selection.VertexIndex = -1;
        ContinuitySolver.ApplyAll(poly);
        ConstraintSolver.Solve(Scene, poly.Id, -1, false);
        Hint = "Usunięto wierzchołek i relacje sąsiednich krawędzi.";
    }

    public void InsertVertexOnSelectedEdge()
    {
        if (Selection.EdgeIndex < 0 || Selection.PolygonId < 0) return;
        var poly = Scene.Get(Selection.PolygonId);
        if (poly == null) return;
        var ei = poly.Wrap(Selection.EdgeIndex);
        var (a, b) = poly.Endpoints(ei);
        var e = poly.E(ei);
        Vec2 pos = e.Kind switch
        {
            SegmentKind.Bezier => Bezier.Eval(a, e.Control1, e.Control2, b, 0.5f),
            SegmentKind.Arc => Arcs.Sample(poly, ei, 24).ElementAt(12),
            _ => Vec2.Lerp(a, b, 0.5f)
        };

        Scene.RemoveConstraintsOf(poly.Id, ei);
        poly.InsertVertexOnEdge(ei, pos);
        Scene.NotifyEdgeInserted(poly.Id, ei);
        Selection.VertexIndex = poly.Wrap(ei + 1);
        Selection.EdgeIndex = -1;
        Hint = "Dodano wierzchołek w środku krawędzi. Relacje tej krawędzi usunięto.";
    }

    public void SetSelectedEdgeKind(SegmentKind kind)
    {
        var (poly, e, a, b) = SelectedEdgeParts();
        if (poly == null || e == null) return;
        if (kind == SegmentKind.Line)
        {
            e.MakeLine();
            Hint = "Krawędź jest odcinkiem.";
            return;
        }
        if (kind == SegmentKind.Bezier)
        {
            e.MakeBezier(a, b);
            ApplyDefaultContinuity(poly, Selection.EdgeIndex);
            ApplyDefaultContinuity(poly, Selection.EdgeIndex + 1);
            ContinuitySolver.ApplyAll(poly);
            Hint = "Bézier (domyślna ciągłość C1, jeśli nie ustawiono ręcznie).";
            return;
        }
        e.MakeArc(a, b);
        ClampArcContinuity(poly, Selection.EdgeIndex);
        ContinuitySolver.ApplyAll(poly);
        Hint = "Łuk okręgu.";
    }

    public void SetSelectedVertexContinuity(Continuity c)
    {
        if (Selection.VertexIndex < 0 || Selection.PolygonId < 0) return;
        var poly = Scene.Get(Selection.PolygonId);
        if (poly == null) return;
        var v = poly.V(Selection.VertexIndex);
        var prev = poly.E(Selection.VertexIndex - 1);
        var next = poly.E(Selection.VertexIndex);
        var touchesArc = prev.Kind == SegmentKind.Arc || next.Kind == SegmentKind.Arc;
        if (touchesArc && c == Continuity.C1) c = Continuity.G1;
        v.Continuity = c;
        v.ContinuitySetByUser = true;
        if (prev.Kind == SegmentKind.Arc) ClampArcContinuity(poly, Selection.VertexIndex - 1);
        if (next.Kind == SegmentKind.Arc) ClampArcContinuity(poly, Selection.VertexIndex);
        ContinuitySolver.ApplyAll(poly);
        Hint = $"Ciągłość: {v.Continuity}";
    }

    public void FlipArcSide()
    {
        var (poly, e, _, _) = SelectedEdgeParts();
        if (poly == null || e == null || e.Kind != SegmentKind.Arc) return;
        e.ArcSide = -e.ArcSide;
        ContinuitySolver.ApplyAll(poly);
    }

    public void AddFixedLengthConstraint(float length)
    {
        if (Selection.EdgeIndex < 0) return;
        Scene.Constraints.Add(new LengthConstraint(Selection.PolygonId, Selection.EdgeIndex, length));
        ConstraintSolver.Solve(Scene, Selection.PolygonId, Selection.EdgeIndex, false);
        Hint = $"Długość krawędzi = {length:F1}";
    }

    public void BeginPickPerpendicular()
    {
        if (Selection.EdgeIndex < 0)
        {
            Hint = "Najpierw wybierz krawędź prawym przyciskiem.";
            return;
        }
        Pending = PendingAction.PickPerpendicularEdge;
        Hint = "Kliknij LPM drugą krawędź (także z innego wielokąta). Esc — anuluj.";
    }

    public void AddPerpendicularConstraint()
    {
        if (Selection.EdgeIndex < 0 || Selection.SecondEdgeIndex < 0)
        {
            Hint = "Potrzebne są dwie krawędzie.";
            return;
        }
        Scene.Constraints.Add(new PairConstraint(
            ConstraintKind.Perpendicular,
            Selection.PolygonId, Selection.EdgeIndex,
            Selection.SecondEdgePolygonId, Selection.SecondEdgeIndex));
        ConstraintSolver.Solve(Scene, Selection.PolygonId, Selection.EdgeIndex, false);
        Hint = "Dodano prostopadłość.";
    }

    public void RemoveConstraintAtSelectedEdge()
    {
        if (Selection.EdgeIndex < 0) return;
        Scene.RemoveConstraintsOf(Selection.PolygonId, Selection.EdgeIndex);
        Hint = "Usunięto relacje zaznaczonej krawędzi.";
    }

    public void RemoveConstraint(Guid id)
    {
        Scene.RemoveConstraint(id);
        Hint = "Usunięto ograniczenie.";
    }

    public float GetSelectedEdgeLength()
    {
        var poly = Scene.Get(Selection.PolygonId);
        if (poly == null || Selection.EdgeIndex < 0) return 0;
        return Arcs.EdgeLength(poly, Selection.EdgeIndex);
    }

    public SegmentKind? SelectedEdgeKind()
    {
        var (_, e, _, _) = SelectedEdgeParts();
        return e?.Kind;
    }

    public bool SelectedVertexTouchesCurve()
    {
        if (Selection.VertexIndex < 0) return false;
        var poly = Scene.Get(Selection.PolygonId);
        if (poly == null) return false;
        var prev = poly.E(Selection.VertexIndex - 1).Kind;
        var next = poly.E(Selection.VertexIndex).Kind;
        return prev is SegmentKind.Bezier or SegmentKind.Arc
               || next is SegmentKind.Bezier or SegmentKind.Arc;
    }

    public bool SelectedVertexTouchesArc()
    {
        if (Selection.VertexIndex < 0) return false;
        var poly = Scene.Get(Selection.PolygonId);
        if (poly == null) return false;
        return poly.E(Selection.VertexIndex - 1).Kind == SegmentKind.Arc
               || poly.E(Selection.VertexIndex).Kind == SegmentKind.Arc;
    }

    private (Poly? poly, Edge? e, Vec2 a, Vec2 b) SelectedEdgeParts()
    {
        if (Selection.EdgeIndex < 0) return (null, null, default, default);
        var poly = Scene.Get(Selection.PolygonId);
        if (poly == null) return (null, null, default, default);
        var (a, b) = poly.Endpoints(Selection.EdgeIndex);
        return (poly, poly.E(Selection.EdgeIndex), a, b);
    }

    private void ApplyDefaultContinuity(Poly poly, int vertex)
    {
        var v = poly.V(vertex);
        if (v.ContinuitySetByUser) return;
        var prev = poly.E(vertex - 1);
        var next = poly.E(vertex);
        var c = Scene.DefaultContinuity;
        if (prev.Kind == SegmentKind.Arc || next.Kind == SegmentKind.Arc)
            c = Continuity.G1;
        v.Continuity = c;
    }

    private static void ClampArcContinuity(Poly poly, int edge)
    {
        var start = poly.V(edge);
        var end = poly.V(edge + 1);
        if (start.Continuity == Continuity.C1) start.Continuity = Continuity.G1;
        if (end.Continuity == Continuity.C1) end.Continuity = Continuity.G1;
        if (start.Continuity != Continuity.G0 && end.Continuity != Continuity.G0)
            end.Continuity = Continuity.G0;
    }
}
