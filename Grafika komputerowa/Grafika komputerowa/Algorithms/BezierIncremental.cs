using Grafika_komputerowa.Model;

namespace Grafika_komputerowa.Algorithms;

/// <summary>
/// Bézier 3. stopnia: baza potęgowa, potem różnice w przód (algorytm przyrostowy).
/// </summary>
public static class BezierIncremental
{
    public static IEnumerable<Point> Rasterize(Vec2 p0, Vec2 p1, Vec2 p2, Vec2 p3, float dt = 0.008f)
    {
        var a0 = p0;
        var a1 = (p1 - p0) * 3f;
        var a2 = (p0 - p1 * 2f + p2) * 3f;
        var a3 = (p3 - p0) + (p1 - p2) * 3f;

        var d0 = a0;
        var d1 = a1 * dt + a2 * (dt * dt) + a3 * (dt * dt * dt);
        var d2 = a2 * (2f * dt * dt) + a3 * (6f * dt * dt * dt);
        var d3 = a3 * (6f * dt * dt * dt);

        Point? last = null;
        for (var t = 0f; t <= 1f + dt * 0.5f; t += dt)
        {
            var pt = new Point((int)MathF.Round(d0.X), (int)MathF.Round(d0.Y));
            if (last == null || last.Value != pt)
            {
                yield return pt;
                last = pt;
            }
            d0 += d1;
            d1 += d2;
            d2 += d3;
        }
    }
}
