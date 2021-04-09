﻿using System.Collections.Generic;
using System.Linq;
using Vascular.Geometry;
using Vascular.Geometry.Bounds;
using Vascular.Geometry.Surfaces;
using Vascular.Geometry.Triangulation;
using Vascular.Intersections.Enforcement;
using Vascular.Structure;

namespace Vascular.Intersections.Triangulation
{
    /// <summary>
    /// Wraps a <see cref="AxialBoundsHashTable{T}"/> of <see cref="TriangleSurfaceTest"/>, allowing networks to be queried.
    /// </summary>
    public class MeshHashTable : IIntersectionEvaluator<TriangleIntersection>
    {
        /// <summary>
        /// 
        /// </summary>
        public AxialBoundsHashTable<TriangleSurfaceTest> Table { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="stride"></param>
        /// <param name="factor"></param>
        public MeshHashTable(Mesh mesh, double stride = 1, double factor = 2)
        {
            this.Table = new AxialBoundsHashTable<TriangleSurfaceTest>(
                mesh.T.Select(t => new TriangleSurfaceTest(t)), stride, factor);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="table"></param>
        public MeshHashTable(AxialBoundsHashTable<TriangleSurfaceTest> table)
        {
            this.Table = table;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="network"></param>
        /// <returns></returns>
        public IEnumerable<TriangleIntersection> Evaluate(Network network)
        {
            var intersections = new List<TriangleIntersection>();
            foreach (var branch in network.Branches)
            {
                this.Table.Query(branch.LocalBounds, triangle =>
                {
                    branch.Query(triangle.GetAxialBounds(), segment =>
                    {
                        var f = 0.0;
                        Vector3 p = null;
                        if (triangle.TestRay(segment.Start.Position, segment.End.Position, segment.Radius, ref f, ref p))
                        {
                            intersections.Add(new TriangleIntersection(segment, triangle, f));
                        }
                    });
                });
            }
            return intersections;
        }
    }
}