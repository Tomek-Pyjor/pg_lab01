using Grafika_komputerowa.Model;

namespace Grafika_komputerowa.Geometry;

/// <summary>
/// Iteracyjne spełnianie relacji (jak w gk_1 / PolygonEditor): start od ruszanego elementu,
/// korekta w obie strony. Wiele relacji na jednej krawędzi jest dozwolone.
/// </summary>
public static class ConstraintSolver
{
    public static void Solve(SceneModel scene, int movedPoly, int movedVertex, bool vertexMoved)
    {
        for (var iter = 0; iter < 12; iter++)
        {
            foreach (var c in scene.Constraints.ToList())
                Satisfy(scene, c, movedPoly, movedVertex, vertexMoved);
            foreach (var p in scene.Polygons)
                ContinuitySolver.ApplyAll(p);
        }
    }

    private static void Satisfy(SceneModel scene, Constraint c, int movedPoly, int movedVertex, bool vertexMoved)
    {
        switch (c)
        {
            case LengthConstraint len:
                ApplyLength(scene, len, movedPoly, movedVertex, vertexMoved);
                break;
            case PairConstraint { Kind: ConstraintKind.Parallel } par:
                ApplyPair(scene, par, rotate90: false, movedPoly, movedVertex, vertexMoved);
                break;
            case PairConstraint { Kind: ConstraintKind.Perpendicular } perp:
                ApplyPair(scene, perp, rotate90: true, movedPoly, movedVertex, vertexMoved);
                break;
        }
    }

    private static void ApplyLength(SceneModel scene, LengthConstraint c, int movedPoly, int movedVertex, bool vertexMoved)
    {
        var p = scene.Get(c.PolygonId);
        if (p == null || c.EdgeIndex < 0) return;
        var i0 = p.Wrap(c.EdgeIndex);
        var i1 = p.Wrap(i0 + 1);
        var a = p.P(i0);
        var b = p.P(i1);
        var dir = b - a;
        if (dir.Length < 1e-6f) dir = new Vec2(1, 0);
        dir = dir.Normalized();

        if (vertexMoved && movedPoly == p.Id)
        {
            var mv = p.Wrap(movedVertex);
            if (mv == i0)
                p.V(i1).Position = a + dir * c.Length;
            else if (mv == i1)
                p.V(i0).Position = b - dir * c.Length;
            else
                StretchFromMid(p, i0, i1, dir, c.Length);
        }
        else
            StretchFromMid(p, i0, i1, dir, c.Length);
    }

    private static void StretchFromMid(Poly p, int i0, int i1, Vec2 dir, float length)
    {
        var mid = Vec2.Lerp(p.P(i0), p.P(i1), 0.5f);
        var h = dir * (length * 0.5f);
        p.V(i0).Position = mid - h;
        p.V(i1).Position = mid + h;
    }

    private static void ApplyPair(SceneModel scene, PairConstraint c, bool rotate90,
        int movedPoly, int movedVertex, bool vertexMoved)
    {
        var pa = scene.Get(c.PolygonA);
        var pb = scene.Get(c.PolygonB);
        if (pa == null || pb == null || c.EdgeA < 0 || c.EdgeB < 0) return;

        var (a0, a1) = pa.Endpoints(c.EdgeA);
        var dirA = (a1 - a0);
        if (dirA.Length < 1e-6f) return;
        dirA = dirA.Normalized();
        if (rotate90) dirA = dirA.Perp();

        AlignEdge(pb, c.EdgeB, dirA, movedPoly, movedVertex, vertexMoved);

        var (b0, b1) = pb.Endpoints(c.EdgeB);
        var dirB = (b1 - b0).Normalized();
        if (rotate90) dirB = dirB.Perp();
        if (dirB.Dot((a1 - a0).Normalized()) < 0) dirB = -dirB;
        AlignEdge(pa, c.EdgeA, dirB, movedPoly, movedVertex, vertexMoved);
    }

    private static void AlignEdge(Poly p, int edge, Vec2 dir, int movedPoly, int movedVertex, bool vertexMoved)
    {
        var i0 = p.Wrap(edge);
        var i1 = p.Wrap(i0 + 1);
        var len = Vec2.Distance(p.P(i0), p.P(i1));
        if (len < 1e-5f) return;
        dir = dir.Normalized();

        if (vertexMoved && movedPoly == p.Id)
        {
            var mv = p.Wrap(movedVertex);
            if (mv == i0)
            {
                p.V(i1).Position = p.P(i0) + dir * len;
                return;
            }
            if (mv == i1)
            {
                p.V(i0).Position = p.P(i1) - dir * len;
                return;
            }
        }

        var start = p.P(i0);
        p.V(i1).Position = start + dir * len;
    }
}
