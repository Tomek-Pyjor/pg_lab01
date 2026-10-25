namespace Grafika_komputerowa.Model;

public readonly struct Vec2
{
    public float X { get; }
    public float Y { get; }

    public Vec2(float x, float y)
    {
        X = x;
        Y = y;
    }

    public static Vec2 From(PointF p) => new(p.X, p.Y);
    public static Vec2 FromPointF(PointF p) => new(p.X, p.Y);
    public static Vec2 From(Point p) => new(p.X, p.Y);
    public Vec2 Perpendicular() => Perp();
    public PointF ToPointF() => new(X, Y);
    public Point ToPoint() => new((int)MathF.Round(X), (int)MathF.Round(Y));

    public static Vec2 operator +(Vec2 a, Vec2 b) => new(a.X + b.X, a.Y + b.Y);
    public static Vec2 operator -(Vec2 a, Vec2 b) => new(a.X - b.X, a.Y - b.Y);
    public static Vec2 operator -(Vec2 v) => new(-v.X, -v.Y);
    public static Vec2 operator *(Vec2 v, float s) => new(v.X * s, v.Y * s);
    public static Vec2 operator *(float s, Vec2 v) => new(v.X * s, v.Y * s);
    public static Vec2 operator /(Vec2 v, float s) => new(v.X / s, v.Y / s);

    public float Length => MathF.Sqrt(X * X + Y * Y);
    public float LengthSquared => X * X + Y * Y;
    public float Dot(Vec2 o) => X * o.X + Y * o.Y;
    public float Cross(Vec2 o) => X * o.Y - Y * o.X;
    public Vec2 Perp() => new(-Y, X);

    public Vec2 Normalized()
    {
        var len = Length;
        return len < 1e-6f ? new Vec2(1, 0) : this / len;
    }

    public static float Distance(Vec2 a, Vec2 b) => (a - b).Length;
    public static Vec2 Lerp(Vec2 a, Vec2 b, float t) => a + (b - a) * t;
}
