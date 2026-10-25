using Grafika_komputerowa.Model;

namespace Grafika_komputerowa.Geometry;

/// <summary>
/// Utrzymuje G0/G1/C1. Przesunięcie punktu kontrolnego rusza drugi uchwyt / sąsiednią krawędź,
/// a nie wierzchołki własnego segmentu (zgodnie ze specyfikacją §6).
/// </summary>
public static class ContinuitySolver
{
    public static void ApplyAll(Poly poly)
    {
        for (var i = 0; i < poly.Count; i++)
            ApplyAtVertex(poly, i, moveVertices: false);
        for (var i = 0; i < poly.Count; i++)
        {
            if (poly.E(i).Kind == SegmentKind.Arc)
                Arcs.FitG1(poly, i);
        }
    }

    public static void AfterVertexMoved(Poly poly, int vertex)
    {
        ApplyAtVertex(poly, vertex, moveVertices: false);
        ApplyAtVertex(poly, vertex - 1, moveVertices: false);
        ApplyAtVertex(poly, vertex + 1, moveVertices: false);
        if (poly.E(vertex).Kind == SegmentKind.Arc) Arcs.FitG1(poly, vertex);
        if (poly.E(vertex - 1).Kind == SegmentKind.Arc) Arcs.FitG1(poly, vertex - 1);
    }

    public static void AfterControlMoved(Poly poly, int edge, bool control1)
    {
        if (control1)
            ApplyAtVertex(poly, edge, moveVertices: true);
        else
            ApplyAtVertex(poly, edge + 1, moveVertices: true);
    }

    public static void ApplyAtVertex(Poly poly, int vi, bool moveVertices)
    {
        vi = poly.Wrap(vi);
        var c = poly.V(vi).Continuity;
        if (c == Continuity.G0) return;

        var prev = poly.E(vi - 1);
        var next = poly.E(vi);
        var joint = poly.P(vi);
        var usesPrev = prev.Kind is SegmentKind.Bezier or SegmentKind.Arc;
        var usesNext = next.Kind is SegmentKind.Bezier or SegmentKind.Arc;
        if (!usesPrev && !usesNext) return;

        var tin = Incoming(poly, vi);
        var tout = Outgoing(poly, vi);
        if (tin.LengthSquared < 1e-10f && tout.LengthSquared < 1e-10f) return;

        if (tin.LengthSquared < 1e-10f) tin = tout;
        if (tout.LengthSquared < 1e-10f) tout = tin;

        if (tin.Dot(tout) < 0)
            tout = -tout;

        if (c == Continuity.G1)
        {
            var dir = tin.LengthSquared >= tout.LengthSquared ? tin.Normalized() : tout.Normalized();
            var inLen = tin.Length;
            var outLen = tout.Length;
            SetIncoming(poly, vi, dir * inLen, moveVertices);
            SetOutgoing(poly, vi, dir * outLen, moveVertices);
        }
        else
        {
            var t = (tin + tout) * 0.5f;
            if (t.LengthSquared < 1e-10f) t = tin;
            SetIncoming(poly, vi, t, moveVertices);
            SetOutgoing(poly, vi, t, moveVertices);
        }
    }

    private static Vec2 Incoming(Poly poly, int vi)
    {
        var e = poly.E(vi - 1);
        var joint = poly.P(vi);
        return e.Kind switch
        {
            SegmentKind.Bezier => joint - e.Control2,
            SegmentKind.Line => joint - poly.P(vi - 1),
            SegmentKind.Arc => ArcTangentAt(poly, vi - 1, atEnd: true),
            _ => joint - poly.P(vi - 1)
        };
    }

    private static Vec2 Outgoing(Poly poly, int vi)
    {
        var e = poly.E(vi);
        var joint = poly.P(vi);
        return e.Kind switch
        {
            SegmentKind.Bezier => e.Control1 - joint,
            SegmentKind.Line => poly.P(vi + 1) - joint,
            SegmentKind.Arc => ArcTangentAt(poly, vi, atEnd: false),
            _ => poly.P(vi + 1) - joint
        };
    }

    private static Vec2 ArcTangentAt(Poly poly, int edge, bool atEnd)
    {
        var (a, b) = poly.Endpoints(edge);
        var r = Arcs.EffectiveRadius(poly, edge);
        if (!Arcs.TryCenter(a, b, r, poly.E(edge).ArcSide, out var c, out _, out var sweep))
            return b - a;
        var p = atEnd ? b : a;
        var radial = p - c;
        var t = radial.Perp();
        if (sweep < 0) t = -t;
        return t;
    }

    private static void SetIncoming(Poly poly, int vi, Vec2 tangent, bool moveVertices)
    {
        var e = poly.E(vi - 1);
        var joint = poly.P(vi);
        if (e.Kind == SegmentKind.Bezier)
            e.Control2 = joint - tangent;
        else if (e.Kind == SegmentKind.Line && moveVertices)
            poly.V(vi - 1).Position = joint - tangent.Normalized() * Vec2.Distance(joint, poly.P(vi - 1));
    }

    private static void SetOutgoing(Poly poly, int vi, Vec2 tangent, bool moveVertices)
    {
        var e = poly.E(vi);
        var joint = poly.P(vi);
        if (e.Kind == SegmentKind.Bezier)
            e.Control1 = joint + tangent;
        else if (e.Kind == SegmentKind.Line && moveVertices)
            poly.V(vi + 1).Position = joint + tangent.Normalized() * Vec2.Distance(joint, poly.P(vi + 1));
    }
}
