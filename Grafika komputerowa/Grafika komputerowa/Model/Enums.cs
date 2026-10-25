namespace Grafika_komputerowa.Model;

public enum SegmentKind
{
    Line,
    Bezier,
    Arc
}

/// <summary>Ciągłość w wierzchołku (jak na rysunku ze specyfikacji: G0 / G1 / C1).</summary>
public enum Continuity
{
    G0,
    G1,
    C1
}

public enum LineDrawMode
{
    Library,
    Bresenham
}

public enum HitKind
{
    None,
    Vertex,
    Control1,
    Control2,
    Edge,
    Polygon,
    ConstraintIcon
}

public enum ConstraintKind
{
    Length,
    Parallel,
    Perpendicular
}
