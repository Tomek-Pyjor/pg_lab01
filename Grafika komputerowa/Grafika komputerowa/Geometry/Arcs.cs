using Grafika_komputerowa.Model;

namespace Grafika_komputerowa.Geometry;

public static class Arcs
{
    public static float EffectiveRadius(Poly poly, int edge)
    {
        var e = poly.E(edge);
        var (a, b) = poly.Endpoints(edge);
        var g0Start = poly.V(edge).Continuity == Continuity.G0;
        var g0End = poly.V(edge + 1).Continuity == Continuity.G0;
        if (g0Start && g0End)
            return Vec2.Distance(a, b) * 0.5f;
        return MathF.Max(e.ArcRadius, Vec2.Distance(a, b) * 0.5f);
    }

    public static bool TryCenter(Vec2 start, Vec2 end, float radius, int side,
        out Vec2 center, out float a0, out float sweep)
    {
        center = default;
        a0 = sweep = 0;
        var chord = end - start;
        var d = chord.Length;
        if (d < 1e-5f) return false;
        if (radius < d * 0.5f - 1e-4f) radius = d * 0.5f;

        var mid = Vec2.Lerp(start, end, 0.5f);
        var h = MathF.Sqrt(MathF.Max(0, radius * radius - (d * 0.5f) * (d * 0.5f)));
        center = mid + chord.Normalized().Perp() * (side * h);
        a0 = MathF.Atan2(start.Y - center.Y, start.X - center.X);
        var a1 = MathF.Atan2(end.Y - center.Y, end.X - center.X);
        sweep = a1 - a0;
        if (side > 0 && sweep < 0) sweep += MathF.PI * 2;
        if (side < 0 && sweep > 0) sweep -= MathF.PI * 2;
        return true;
    }

    public static IEnumerable<Vec2> Sample(Poly poly, int edge, int steps = 48)
    {
        var e = poly.E(edge);
        var (a, b) = poly.Endpoints(edge);
        var r = EffectiveRadius(poly, edge);
        if (!TryCenter(a, b, r, e.ArcSide, out var c, out var a0, out var sweep))
        {
            yield return a;
            yield return b;
            yield break;
        }
        for (var i = 0; i <= steps; i++)
        {
            var t = i / (float)steps;
            var ang = a0 + sweep * t;
            yield return new Vec2(c.X + MathF.Cos(ang) * r, c.Y + MathF.Sin(ang) * r);
        }
    }

    /// <summary>
    /// G1 przy łuku: styczna w końcu G1 zgodna z sąsiednią krawędzią.
    /// Środek leży na prostej prostopadłej do stycznej.
    /// </summary>
    public static void FitG1(Poly poly, int edge)
    {
        var e = poly.E(edge);
        if (e.Kind != SegmentKind.Arc) return;
        var start = poly.P(edge);
        var end = poly.P(edge + 1);
        var cStart = poly.V(edge).Continuity;
        var cEnd = poly.V(edge + 1).Continuity;

        if (cStart != Continuity.G0 && cEnd != Continuity.G0)
        {
            poly.V(edge + 1).Continuity = Continuity.G0;
            cEnd = Continuity.G0;
        }

        if (cStart == Continuity.G0 && cEnd == Continuity.G0)
        {
            e.ArcRadius = Vec2.Distance(start, end) * 0.5f;
            return;
        }

        Vec2 joint, far, tangent;
        if (cStart != Continuity.G0)
        {
            joint = start;
            far = end;
            tangent = OutgoingTangent(poly, edge - 1, atEnd: true);
        }
        else
        {
            joint = end;
            far = start;
            tangent = IncomingTangent(poly, edge + 1, atStart: true);
        }

        if (tangent.LengthSquared < 1e-8f) return;
        var n = tangent.Normalized().Perp();
        var v = far - joint;
        foreach (var s in new[] { 1f, -1f })
        {
            var denom = 2f * s * n.Dot(v);
            if (MathF.Abs(denom) < 1e-5f) continue;
            var r = v.LengthSquared / denom;
            if (r > Vec2.Distance(start, end) * 0.5f + 0.1f)
            {
                e.ArcRadius = r;
                e.ArcSide = s > 0 ? 1 : -1;
                return;
            }
        }
    }

    public static Vec2 OutgoingTangent(Poly poly, int edge, bool atEnd)
    {
        edge = poly.Wrap(edge);
        var e = poly.E(edge);
        var (a, b) = poly.Endpoints(edge);
        return e.Kind switch
        {
            SegmentKind.Bezier when atEnd => b - e.Control2,
            SegmentKind.Bezier => e.Control1 - a,
            _ => b - a
        };
    }

    public static Vec2 IncomingTangent(Poly poly, int edge, bool atStart)
    {
        edge = poly.Wrap(edge);
        var e = poly.E(edge);
        var (a, b) = poly.Endpoints(edge);
        return e.Kind switch
        {
            SegmentKind.Bezier when atStart => e.Control1 - a,
            SegmentKind.Bezier => b - e.Control2,
            _ => b - a
        };
    }

    public static float EdgeLength(Poly poly, int edge)
    {
        var e = poly.E(edge);
        var (a, b) = poly.Endpoints(edge);
        return e.Kind switch
        {
            SegmentKind.Bezier => Bezier.Length(a, e.Control1, e.Control2, b),
            SegmentKind.Arc => ArcLength(poly, edge),
            _ => Vec2.Distance(a, b)
        };
    }

    private static float ArcLength(Poly poly, int edge)
    {
        var r = EffectiveRadius(poly, edge);
        var (a, b) = poly.Endpoints(edge);
        if (!TryCenter(a, b, r, poly.E(edge).ArcSide, out _, out _, out var sweep))
            return Vec2.Distance(a, b);
        return MathF.Abs(sweep) * r;
    }
}
