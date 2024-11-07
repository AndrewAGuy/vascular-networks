using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Vascular.Geometry;
using Vascular.Geometry.Bounds;
using Vascular.Structure;

namespace Vascular.Functionality.Artificial;

public class TemplatedVesselSource : IAxialBoundsQueryable<Segment>
{
    public TemplatedVesselSource(Template t, IEnumerable<Vector3> P)
    {
        template = t;
        placements = new AxialBoundsHashTable<PosedTemplate>(
            P.Select(p => new PosedTemplate(p, template.Orientation(p), template.Channels))
            );
    }

    public void Query(AxialBounds query, Action<Segment> action)
    {
        placements.Query(query, pose =>
        {
            foreach (var s in Generate(pose))
            {
                s.GenerateBounds();
                if (query.Intersects(s.Bounds))
                    action(s);
            }
        });
    }

    public bool Query(AxialBounds query, Func<Segment, bool> action)
    {
        return placements.Query(query, pose =>
        {
            foreach (var s in Generate(pose))
            {
                s.GenerateBounds();
                if (query.Intersects(s.Bounds))
                    if (action(s))
                        return true;
            }
            return false;
        });
    }

    public AxialBounds GetAxialBounds() => placements.GetAxialBounds();

    public IEnumerator<Segment> GetEnumerator()
    {
        foreach (var p in placements)
            foreach (var s in Generate(p))
                yield return s;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private readonly Template template;

    private readonly IAxialBoundsQueryable<PosedTemplate> placements;

    private IEnumerable<Segment> Generate(PosedTemplate pose)
    {
        var M = pose.Orientation;
        var v = pose.Position;
        Vector3 tf(Vector3 x) => M * x + v;
        foreach (var s in template.Channels)
        {
            yield return Segment.MakeDummy(tf(s.Start.Position), tf(s.End.Position), s.Radius);
        }
    }
}

internal class PosedTemplate : IAxialBoundable
{
    public PosedTemplate(Vector3 p, Matrix3 M, IEnumerable<Segment> S)
    {
        this.Position = p;
        this.Orientation = M;
        Vector3 tf(Vector3 x) => M * x + p;
        this.Bounds = new();
        foreach (var s in S)
        {
            var b = new AxialBounds(tf(s.Start.Position))
                .Append(tf(s.End.Position))
                .Extend(s.Radius);
            this.Bounds.Append(b);
        }
    }

    public Vector3 Position { get; }
    public Matrix3 Orientation { get; }
    private AxialBounds Bounds { get; }

    public AxialBounds GetAxialBounds() => this.Bounds;
}
