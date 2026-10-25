namespace Grafika_komputerowa.Model;

public sealed class SceneModel
{
    private int _nextId = 1;
    public List<Poly> Polygons { get; } = [];
    public List<Constraint> Constraints { get; } = [];

    /// <summary>L–Z: C1. Osoby A–K: ustaw G1.</summary>
    public Continuity DefaultContinuity { get; set; } = Continuity.C1;

    public Poly AddPolygon(IEnumerable<Vec2> points)
    {
        var p = new Poly(_nextId++);
        foreach (var pt in points)
            p.Vertices.Add(new Vertex(pt));
        for (var i = 0; i < p.Count; i++)
            p.Edges.Add(new Edge());
        Polygons.Add(p);
        return p;
    }

    public Poly? Get(int id) => Polygons.FirstOrDefault(p => p.Id == id);

    public void RemoveConstraintsOf(int polygonId, int edgeIndex)
    {
        Constraints.RemoveAll(c => c.Uses(polygonId, edgeIndex));
    }

    public void NotifyEdgeInserted(int polygonId, int insertedAfter)
    {
        foreach (var c in Constraints)
            c.OnEdgeInserted(polygonId, insertedAfter);
    }

    public void NotifyEdgeRemoved(int polygonId, int removedEdge, int oldCount)
    {
        foreach (var c in Constraints)
            c.OnEdgeRemoved(polygonId, removedEdge, oldCount);
        CleanupInvalidConstraints();
    }

    public void RemoveConstraint(Guid id) => Constraints.RemoveAll(c => c.Id == id);

    public void CleanupInvalidConstraints()
    {
        Constraints.RemoveAll(c =>
            c is LengthConstraint { EdgeIndex: < 0 } ||
            c is PairConstraint p && (p.EdgeA < 0 || p.EdgeB < 0));
    }
}
