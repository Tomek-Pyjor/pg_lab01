using Grafika_komputerowa.Geometry;
using Grafika_komputerowa.Model;

namespace Grafika_komputerowa.Scene;

/// <summary>
/// Scena startowa. Nowy wielokąt = jedna linia AddPolygon([...]).
/// </summary>
public static class DemoScene
{
    public static void Build(SceneModel scene)
    {
        var a = scene.AddPolygon(
        [
            new Vec2(160, 420),
            new Vec2(420, 400),
            new Vec2(240, 140)
        ]);
        a.E(2).MakeBezier(a.P(2), a.P(0));
        a.E(2).Control1 = new Vec2(80, 180);
        a.E(2).Control2 = new Vec2(70, 340);
        a.V(2).Continuity = Continuity.C1;
        a.V(2).ContinuitySetByUser = true;
        a.V(0).Continuity = Continuity.C1;
        a.V(0).ContinuitySetByUser = true;
        ContinuitySolver.ApplyAll(a);

        var b = scene.AddPolygon(
        [
            new Vec2(580, 160),
            new Vec2(860, 160),
            new Vec2(580, 420)
        ]);

        scene.Constraints.Add(new LengthConstraint(b.Id, 0, Arcs.EdgeLength(b, 0)));
        scene.Constraints.Add(new PairConstraint(ConstraintKind.Perpendicular, b.Id, 0, b.Id, 2));
        ConstraintSolver.Solve(scene, b.Id, 0, true);
    }
}
