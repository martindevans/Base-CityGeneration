using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Base_CityGeneration.Datastructures.HalfEdge;
using EpimetheusPlugins.Extensions;
using System.Numerics;
using Myre.Extensions;
using SwizzleMyVectors.Geometry;

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

            AngularDeviation = CalculateAngularVariance(f.Edges);
        }

        #region angular variance
        private static float CalculateAngularVariance(IEnumerable<HalfEdge<FloorplanVertexTag, FloorplanHalfEdgeTag, FloorplanFaceTag>> edges)
        {
            ////var smallAngles = edges
            ////    .Select(ab => Vector2.Dot(ab.Segment.Line.Direction, -ab.Next.Segment.Line.Direction))
            ////    .Where(dot => dot < 0)
            ////    .Select(dot => -dot)
            ////    .Append(0f)
            ////    .Sum();

            ////if (Math.Abs(smallAngles) < float.Epsilon)
            ////    return 0;

            ////return smallAngles / edges.Count();

            var dots = edges.Select(ab => Vector2.Dot(ab.Segment.Line.Direction, -ab.Next.Segment.Line.Direction)).ToArray();
            return CalculateAngularVariance(dots);

            ////calculate dot product between lines
            //return (float)Math.Sqrt(edges
            //    .Select(ab => Vector2.Dot(ab.Segment.Line.Direction, -ab.Next.Segment.Line.Direction)) //Measure angle between lines
            //    .Where(dot => !dot.TolerantEquals(-1, 0.015192f)) //Exclude angles which are nearly 180 degrees (to within 10 degrees)
            //    .Select(a => (float)Math.Acos(a))
            //    .Variance()
            //);
        }

        public static float CalculateAngularVariance(IEnumerable<Vertex<FloorplanVertexTag, FloorplanHalfEdgeTag, FloorplanFaceTag>> vertices)
        {
            //Zip with the next vertex around and calculate line segments
            var edges = vertices.Zip(vertices.Skip(1).Concat(vertices.Take(1)), (a, b) => new LineSegment2(a.Position, b.Position)).ToArray();

            //Calculate dot product at each corner
            var dots = edges.ZipWithIndex().Select(abi => {
                var ab = abi.Value;
                var bc = edges[(abi.Key + 1) % edges.Length];
                return Vector2.Dot(ab.Line.Direction, -bc.Line.Direction);
            });

            return CalculateAngularVariance(dots);
        }

        private static float CalculateAngularVariance(IEnumerable<float> dots)
        {
            var small = 0f;
            var ok = 1;
            foreach (var dot in dots)
            {
                if (dot >= 0)
                {
                    //Ignore straight lines (dot == 1 indicates a straight line with a vertex in the middle)
                    if (dot < 0.99f)
                        ok++;
                }
                else
                    small += (float)Math.Pow(-dot, 0.1f);
            }

            return small / ok;
        }
        #endregion

        #region length variance
        ////Length variance is a bad measure!
        ////
        //// #----------#
        //// |          |
        //// |        #-#
        //// |        |
        //// |        |
        //// #--------#
        ////
        //// This shape is not particularly weird, but that tiny kink has a very high variance because all the other walls are very long
        //public static float CalculateLengthVariance(IEnumerable<Vector2> vertices)
        //{
        //    Contract.Requires(vertices != null);

        //    //Each vertex has a value = average distance from all other vertices
        //    //Calculate the variance of this value
        //    var count = vertices.Count();

        //    //Variance in average length
        //    var variance = vertices
        //        .Select(a => vertices.Select(b => Vector2.Distance(a, b)).Sum() / count)
        //        .Variance();

        //    //Normalize into dimensionless range:
        //    // Sqrt(Variance) returns deviation (dimension in meters)
        //    // Sqrt(Area) is in meters
        //    // Sqrt(Variance / Area) is therefore dimensionless

        //    return (float)Math.Sqrt(
        //        variance / vertices.Area()
        //    );
        //}

        //public static float CalculateLengthVariance(IEnumerable<Vertex<FloorplanVertexTag, FloorplanHalfEdgeTag, FloorplanFaceTag>> vertices)
        //{
        //    Contract.Requires(vertices != null);

        //    return CalculateLengthVariance(vertices.Select(a => a.Position));
        //}
        #endregion
    }
}
