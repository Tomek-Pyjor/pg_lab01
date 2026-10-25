namespace Grafika_komputerowa.Model;

public abstract class Constraint
{
    public Guid Id { get; } = Guid.NewGuid();
    public abstract ConstraintKind Kind { get; }
    public abstract bool Uses(int polygonId, int edgeIndex);
    public abstract void OnEdgeInserted(int polygonId, int insertedAfter);
    public abstract void OnEdgeRemoved(int polygonId, int removedEdge, int oldCount);
}

public sealed class LengthConstraint : Constraint
{
    public int PolygonId { get; }
    public int EdgeIndex { get; set; }
    public float Length { get; set; }
    public override ConstraintKind Kind => ConstraintKind.Length;

    public LengthConstraint(int polygonId, int edgeIndex, float length)
    {
        PolygonId = polygonId;
        EdgeIndex = edgeIndex;
        Length = length;
    }

    public override bool Uses(int polygonId, int edgeIndex) =>
        PolygonId == polygonId && EdgeIndex == edgeIndex;

    public override void OnEdgeInserted(int polygonId, int insertedAfter)
    {
        if (PolygonId == polygonId && EdgeIndex > insertedAfter)
            EdgeIndex++;
    }

    public override void OnEdgeRemoved(int polygonId, int removedEdge, int oldCount)
    {
        if (PolygonId != polygonId) return;
        if (EdgeIndex == removedEdge)
            EdgeIndex = -1;
        else if (EdgeIndex > removedEdge)
            EdgeIndex--;
    }
}

public sealed class PairConstraint : Constraint
{
    public int PolygonA { get; }
    public int EdgeA { get; set; }
    public int PolygonB { get; }
    public int EdgeB { get; set; }
    public override ConstraintKind Kind { get; }

    public PairConstraint(ConstraintKind kind, int pa, int ea, int pb, int eb)
    {
        Kind = kind;
        PolygonA = pa;
        EdgeA = ea;
        PolygonB = pb;
        EdgeB = eb;
    }

    public override bool Uses(int polygonId, int edgeIndex) =>
        (PolygonA == polygonId && EdgeA == edgeIndex) ||
        (PolygonB == polygonId && EdgeB == edgeIndex);

    public override void OnEdgeInserted(int polygonId, int insertedAfter)
    {
        if (PolygonA == polygonId && EdgeA > insertedAfter) EdgeA++;
        if (PolygonB == polygonId && EdgeB > insertedAfter) EdgeB++;
    }

    public override void OnEdgeRemoved(int polygonId, int removedEdge, int oldCount)
    {
        if (PolygonA == polygonId)
        {
            if (EdgeA == removedEdge) EdgeA = -1;
            else if (EdgeA > removedEdge) EdgeA--;
        }
        if (PolygonB == polygonId)
        {
            if (EdgeB == removedEdge) EdgeB = -1;
            else if (EdgeB > removedEdge) EdgeB--;
        }
    }
}
