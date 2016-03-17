﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Base_CityGeneration.Datastructures.HalfEdge;
using EpimetheusPlugins.Extensions;
using System.Numerics;
using Myre.Extensions;
using Placeholder.ConstructiveSolidGeometry;
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
        public float Convexity { get; private set; }

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

            AngularDeviation = CalculateAngularDeviation(f.Edges);
            Convexity = CalculateConvexity(f.Vertices.Select(v => v.Position));
        }

        #region convexity
        private static float CalculateConvexity(IEnumerable<Vector2> vertices)
        {
            var area = vertices.Area();
            var convexHullArea = vertices.ConvexHull().Area();

            return area / convexHullArea ;
        }

        public static float CalculateConvexity(IEnumerable<Vertex<FloorplanVertexTag, FloorplanHalfEdgeTag, FloorplanFaceTag>> vertices)
        {
            return CalculateConvexity(vertices.Select(v => v.Position));
        }
        #endregion

        #region angular deviation
        private static float CalculateAngularDeviation(IEnumerable<HalfEdge<FloorplanVertexTag, FloorplanHalfEdgeTag, FloorplanFaceTag>> edges)
        {
            Contract.Requires(edges != null);

            return CalculateAngularDeviation(edges
                .Select(ab => Vector2.Dot(ab.Segment.Line.Direction, ab.Next.Segment.Line.Direction))
            );
        }

        public static float CalculateAngularDeviation(IEnumerable<Vertex<FloorplanVertexTag, FloorplanHalfEdgeTag, FloorplanFaceTag>> vertices)
        {
            Contract.Requires(vertices != null);

            //Zip with the next vertex around and calculate line segments
            var edges = vertices.Zip(vertices.Skip(1).Concat(vertices.Take(1)), (a, b) => new LineSegment2(a.Position, b.Position)).ToArray();

            //Calculate dot product at each corner
            var dots = edges.ZipWithIndex().Select(abi => {
                var ab = abi.Value;
                var bc = edges[(abi.Key + 1) % edges.Length];
                return Vector2.Dot(ab.Line.Direction, bc.Line.Direction);
            });

            return CalculateAngularDeviation(dots);
        }

        private static float CalculateAngularDeviation(IEnumerable<float> dots)
        {
            return (float)Math.Sqrt(
                dots
                    .Where(dot => !dot.TolerantEquals(1, 0.015192f)) //Exclude angles which are nearly 0 degrees (to within 10 degrees)
                    .Select(a => (float)Math.Acos(a))
                    .Variance()
            );
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
