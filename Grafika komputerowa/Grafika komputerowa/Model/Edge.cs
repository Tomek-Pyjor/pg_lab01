namespace Grafika_komputerowa.Model;

public sealed class Edge
{
    public SegmentKind Kind { get; set; } = SegmentKind.Line;
    public Vec2 Control1 { get; set; }
    public Vec2 Control2 { get; set; }

    /// <summary>Promień łuku. Przy obu końcach G0 wymuszany jest chord/2.</summary>
    public float ArcRadius { get; set; }
    public int ArcSide { get; set; } = 1;

    public void MakeBezier(Vec2 start, Vec2 end)
    {
        Kind = SegmentKind.Bezier;
        Control1 = Vec2.Lerp(start, end, 1f / 3f);
        Control2 = Vec2.Lerp(start, end, 2f / 3f);
    }

    public void MakeArc(Vec2 start, Vec2 end)
    {
        Kind = SegmentKind.Arc;
        ArcRadius = Vec2.Distance(start, end) * 0.5f;
        ArcSide = 1;
    }

    public void MakeLine() => Kind = SegmentKind.Line;
}
