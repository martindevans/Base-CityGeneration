using System;
using System.Collections.Generic;
using System.Linq;
using Base_CityGeneration.Datastructures.HalfEdge;
using EpimetheusPlugins.Extensions;
using System.Numerics;
using MathHelper = Microsoft.Xna.Framework.MathHelper;

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

        public override void Attach(Face<FloorplanVertexTag, FloorplanHalfEdgeTag, FloorplanFaceTag> f)
        {
            base.Attach(f);

            LengthDeviation = CalculateLengthVariance(f.Vertices.Select(a => a.Position));
            AngularDeviation = CalculateAngularVariance(f.Edges);
        }

        private static float CalculateAngularVariance(IEnumerable<HalfEdge<FloorplanVertexTag, FloorplanHalfEdgeTag, FloorplanFaceTag>> edges)
        {
            //calculate dot product between lines (clamped to 0.707 -> 1 range, i.e. 45 -> 90 degrees)
            return (float)Math.Sqrt(edges
                .Select(ab => MathHelper.Clamp(Vector2.Dot(ab.Segment.Line.Direction, -ab.Next.Segment.Line.Direction), 0, 1))
                .Variance()
            );
        }

        private static float CalculateLengthVariance(IEnumerable<Vector2> vertices)
        {
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
