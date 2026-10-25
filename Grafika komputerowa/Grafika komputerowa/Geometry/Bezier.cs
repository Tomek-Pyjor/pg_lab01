using Grafika_komputerowa.Model;

namespace Grafika_komputerowa.Geometry;

public static class Bezier
{
    public static Vec2 Eval(Vec2 p0, Vec2 p1, Vec2 p2, Vec2 p3, float t)
    {
        var u = 1 - t;
        return p0 * (u * u * u) + p1 * (3 * u * u * t) + p2 * (3 * u * t * t) + p3 * (t * t * t);
    }

    public static Vec2 TangentStart(Vec2 p0, Vec2 p1) => p1 - p0;
    public static Vec2 TangentEnd(Vec2 p2, Vec2 p3) => p3 - p2;

    public static float Length(Vec2 p0, Vec2 p1, Vec2 p2, Vec2 p3, int n = 32)
    {
        var s = 0f;
        var prev = p0;
        for (var i = 1; i <= n; i++)
        {
            var p = Eval(p0, p1, p2, p3, i / (float)n);
            s += Vec2.Distance(prev, p);
            prev = p;
        }
        return s;
    }

    public static Vec2 ClosestOnLine(Vec2 p, Vec2 a, Vec2 b)
    {
        var ab = b - a;
        var l2 = ab.LengthSquared;
        if (l2 < 1e-10f) return a;
        var t = Math.Clamp(ab.Dot(p - a) / l2, 0, 1);
        return Vec2.Lerp(a, b, t);
    }
}
