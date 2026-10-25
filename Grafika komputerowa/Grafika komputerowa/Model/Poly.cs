namespace Grafika_komputerowa.Model;

public sealed class Vertex
{
    public Vec2 Position { get; set; }
    public Continuity Continuity { get; set; } = Continuity.G0;
    public bool ContinuitySetByUser { get; set; }

    public Vertex(Vec2 position) => Position = position;
}

public sealed class Poly
{
    public int Id { get; }
    public bool IsSelected { get; set; }
    public List<Vertex> Vertices { get; } = [];
    public List<Edge> Edges { get; } = [];

    public Poly(int id) => Id = id;

    public int Count => Vertices.Count;

    public int Wrap(int i)
    {
        var n = Count;
        return ((i % n) + n) % n;
    }

    public Vertex V(int i) => Vertices[Wrap(i)];
    public Edge E(int i) => Edges[Wrap(i)];
    public Vec2 P(int i) => V(i).Position;

    public (Vec2 a, Vec2 b) Endpoints(int edge)
    {
        edge = Wrap(edge);
        return (Vertices[edge].Position, Vertices[Wrap(edge + 1)].Position);
    }

    public void Translate(Vec2 d)
    {
        foreach (var v in Vertices)
            v.Position += d;
        foreach (var e in Edges)
        {
            if (e.Kind == SegmentKind.Bezier)
            {
                e.Control1 += d;
                e.Control2 += d;
            }
        }
    }

    public void InsertVertexOnEdge(int edge, Vec2 position)
    {
        edge = Wrap(edge);
        Vertices.Insert(edge + 1, new Vertex(position));
        Edges.Insert(edge + 1, new Edge());
        E(edge).MakeLine();
    }

    public bool RemoveVertex(int vertex)
    {
        if (Count <= 3) return false;
        vertex = Wrap(vertex);
        Vertices.RemoveAt(vertex);
        Edges.RemoveAt(vertex);
        E(vertex - 1).MakeLine();
        return true;
    }
}
