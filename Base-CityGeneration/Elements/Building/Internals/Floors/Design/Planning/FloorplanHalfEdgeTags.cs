using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Base_CityGeneration.Datastructures.HalfEdge;
using EpimetheusPlugins.Extensions;
using System.Numerics;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Design.Planning
{
    public class FloorplanVertexTag
    {
    }

    public class FloorplanHalfEdgeTag
    {
        public bool IsImpassable { get; private set; }

        public FloorplanHalfEdgeTag(bool isImpassable)
        {
            IsImpassable = isImpassable;
        }
    }

    public class FloorplanFaceTag
        : BaseFaceTag<FloorplanVertexTag, FloorplanHalfEdgeTag, FloorplanFaceTag>
    {
        public float LengthDeviation { get; private set; }
        public float AngularDeviation { get; private set; }

        public bool Mergeable { get; private set; }

        public FloorplanFaceTag(bool mergeable)
        {
            //todo: pass in information indicating if this already has a spacespec assigned (e.g. vertical element)
            //Use this data instead of passing in a bool directly
            Mergeable = mergeable;
        }

        public override void Attach(Face<FloorplanVertexTag, FloorplanHalfEdgeTag, FloorplanFaceTag> f)
        {
            base.Attach(f);

            LengthDeviation = CalculateLengthVariance(f.Vertices.Select(a => a.Position));
            AngularDeviation = CalculateAngularVariance(f.Edges);
        }

        private static float CalculateAngularVariance(IEnumerable<HalfEdge<FloorplanVertexTag, FloorplanHalfEdgeTag, FloorplanFaceTag>> edges)
        {
            //calculate dot product between lines
            return (float)Math.Sqrt(edges
                .Select(ab => Vector2.Dot(ab.Segment.Line.Direction, -ab.Next.Segment.Line.Direction)) //Measure angle between lines
                .Where(dot => !dot.TolerantEquals(-1, 0.015192f)) //Exclude angles which are nearly 180 degrees (to within 10 degrees)
                .Variance()
            );
        }

        private static float CalculateLengthVariance(IEnumerable<Vector2> vertices)
        {
            Contract.Requires(vertices != null);

            //Each vertex has a value = average distance from all other vertices
            //Calculate the variance of this value
            var count = vertices.Count();
            return (float)Math.Sqrt(vertices
                .Select(a => vertices.Select(b => Vector2.Distance(a, b)).Sum() / count)
                .Variance()
            );
        }
    }
}
