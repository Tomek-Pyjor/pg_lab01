using Grafika_komputerowa.Geometry;
using Grafika_komputerowa.Model;

namespace Grafika_komputerowa.Interaction;

public readonly record struct Hit(
    HitKind Kind,
    int PolygonId = -1,
    int Index = -1,
    Guid? ConstraintId = null);

public static class HitTest
{
    public const float VertexR = 9f;
    public const float ControlR = 8f;
    public const float EdgeR = 7f;
    public const float IconR = 14f;

    public static Hit Pick(SceneModel scene, Vec2 p)
    {
        foreach (var poly in scene.Polygons)
        {
            for (var i = 0; i < poly.Count; i++)
            {
                var e = poly.E(i);
                if (e.Kind != SegmentKind.Bezier) continue;
                if (Vec2.Distance(p, e.Control1) <= ControlR)
                    return new Hit(HitKind.Control1, poly.Id, i);
                if (Vec2.Distance(p, e.Control2) <= ControlR)
                    return new Hit(HitKind.Control2, poly.Id, i);
            }
        }

        foreach (var poly in scene.Polygons)
        {
            for (var i = 0; i < poly.Count; i++)
            {
                if (Vec2.Distance(p, poly.P(i)) <= VertexR)
                    return new Hit(HitKind.Vertex, poly.Id, i);
            }
        }

        foreach (var c in scene.Constraints)
        {
            foreach (var icon in ConstraintIcons.Positions(scene, c))
            {
                if (Vec2.Distance(p, icon) <= IconR)
                    return new Hit(HitKind.ConstraintIcon, ConstraintId: c.Id);
            }
        }

        foreach (var poly in scene.Polygons)
        {
            for (var i = 0; i < poly.Count; i++)
            {
                if (DistEdge(poly, i, p) <= EdgeR)
                    return new Hit(HitKind.Edge, poly.Id, i);
            }
        }

        foreach (var poly in scene.Polygons)
        {
            if (Inside(p, poly))
                return new Hit(HitKind.Polygon, poly.Id);
        }

        return new Hit(HitKind.None);
    }

    public static float DistEdge(Poly poly, int i, Vec2 p)
    {
        var e = poly.E(i);
        var (a, b) = poly.Endpoints(i);
        return e.Kind switch
        {
            SegmentKind.Line => Vec2.Distance(p, Bezier.ClosestOnLine(p, a, b)),
            SegmentKind.Bezier => DistBezier(p, a, e.Control1, e.Control2, b),
            SegmentKind.Arc => DistPoly(p, Arcs.Sample(poly, i)),
            _ => float.MaxValue
        };
    }

    private static float DistBezier(Vec2 p, Vec2 p0, Vec2 p1, Vec2 p2, Vec2 p3)
    {
        var best = float.MaxValue;
        for (var i = 0; i <= 40; i++)
            best = MathF.Min(best, Vec2.Distance(p, Bezier.Eval(p0, p1, p2, p3, i / 40f)));
        return best;
    }

    private static float DistPoly(Vec2 p, IEnumerable<Vec2> pts)
    {
        Vec2? prev = null;
        var best = float.MaxValue;
        foreach (var q in pts)
        {
            if (prev != null)
                best = MathF.Min(best, Vec2.Distance(p, Bezier.ClosestOnLine(p, prev.Value, q)));
            prev = q;
        }
        return best;
    }

    private static bool Inside(Vec2 p, Poly poly)
    {
        var inside = false;
        var n = poly.Count;
        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            var pi = poly.P(i);
            var pj = poly.P(j);
            if ((pi.Y > p.Y) != (pj.Y > p.Y) &&
                p.X < (pj.X - pi.X) * (p.Y - pi.Y) / ((pj.Y - pi.Y) + 1e-8f) + pi.X)
                inside = !inside;
        }
        return inside;
    }
}

public static class ConstraintIcons
{
    public static IEnumerable<Vec2> Positions(SceneModel scene, Constraint c)
    {
        switch (c)
        {
            case LengthConstraint len when scene.Get(len.PolygonId) is { } p:
                yield return Mid(p, len.EdgeIndex);
                break;
            case PairConstraint pair:
                if (scene.Get(pair.PolygonA) is { } pa)
                    yield return Mid(pa, pair.EdgeA);
                if (scene.Get(pair.PolygonB) is { } pb)
                    yield return Mid(pb, pair.EdgeB);
                break;
        }
    }

    public static Vec2 Mid(Poly p, int edge)
    {
        var (a, b) = p.Endpoints(edge);
        var e = p.E(edge);
        if (e.Kind == SegmentKind.Bezier)
            return Bezier.Eval(a, e.Control1, e.Control2, b, 0.5f);
        if (e.Kind == SegmentKind.Arc)
            return Arcs.Sample(p, edge, 24).ElementAt(12);
        return Vec2.Lerp(a, b, 0.5f);
    }
}
