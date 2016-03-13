using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Contracts;
using Base_CityGeneration.Datastructures.HalfEdge;
using EpimetheusPlugins.Procedural.Utilities;
using System.Numerics;
using System.Linq;
using Myre.Extensions;

namespace Base_CityGeneration.Elements.Roads
{
    public class FaceBlockBuilder : IFaceBuilder
    {
        public Face<IVertexBuilder, IHalfEdgeBuilder, IFaceBuilder> Face { get; private set; }

        private ReadOnlyCollection<Vector2> _footprint = null;
        public ReadOnlyCollection<Vector2> Shape
        {
            get
            {
                if (_footprint == null)
                    _footprint = CalculateShape();
                return _footprint;
            }
        }

        public FaceBlockBuilder(Face<IVertexBuilder, IHalfEdgeBuilder, IFaceBuilder> face)
        {
            Face = face;
        }

        private ReadOnlyCollection<Vector2> CalculateShape()
        {
            Contract.Requires(Face != null && Face.Edges != null);

            //return new ReadOnlyCollection<Vector2>((
            //    from halfEdge in Face.Edges
            //    let builder = halfEdge.BuilderEndingWith(halfEdge.EndVertex)
            //    from point in new[] { builder.RightStart, builder.RightEnd }
            //    select point
            //).ToArray());

            //Get the builders around this face
            var builders = Face.Edges.Select(e => e.BuilderEndingWith(e.EndVertex)).ToArray();

            var points = new List<Vector2>();

            for (var i = 0; i < builders.Length; i++)
            {
                //Get roads in and out from this junction
                var b = builders[i];
                var n = builders[(i + 1) % builders.Length];

                //Get the junction shape
                var j = b.HalfEdge.EndVertex.Tag.Shape;

                //b.RightEnd and n.RightStart are the points on this junction, adjacent to this block
                //Trace a path along the boundary between these points, use the path which does *not* include the other points
                Vector2[] cwp, ccwp;
                bool cw, ccw;
                j.TraceConnectingPath(
                    b.RightEnd,
                    n.RightStart,
                    0.01f, out cwp, out cw, out ccwp, out ccw,
                    b.LeftEnd, n.LeftStart
                );

                //Sanity check
                if (!cw && !ccw)
                    return null;

                //Add points
                points.AddRange(cw ? cwp : ccwp);
            }

            return new ReadOnlyCollection<Vector2>(points);
        }
    }
}
