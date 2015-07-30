using System.Collections.Generic;
using Base_CityGeneration.Datastructures.HalfEdge;
using Base_CityGeneration.Elements.Generic;
using Base_CityGeneration.Styles;
using EpimetheusPlugins.Extensions;
using EpimetheusPlugins.Procedural;
using EpimetheusPlugins.Procedural.Utilities;
using EpimetheusPlugins.Scripts;
using Microsoft.Xna.Framework;
using Myre.Collections;
using Myre.Extensions;
using System;
using System.Linq;

namespace Base_CityGeneration.Elements.Roads
{
    [Script("0C1517AB-2231-45BF-84E3-85E4780AE852", "Basic Road Junction")]
    public class BasicJunction
        :ProceduralScript, IJunction
    {
        public float GroundHeight { get; set; }

        public Vertex<IVertexBuilder, IHalfEdgeBuilder, IFaceBuilder> Vertex { get; set; }

        public override bool Accept(Prism bounds, INamedDataProvider parameters)
        {
            return true;
        }

        public override void Subdivide(Prism bounds, ISubdivisionGeometry geometry, INamedDataCollection hierarchicalParameters)
        {
            this.CreateFlatPlane(geometry, "tarmac", bounds.Footprint, 1, -1);

            CreateFootpaths(bounds, geometry, hierarchicalParameters, hierarchicalParameters.RoadSidewalkMaterial(Random), HierarchicalParameters.RoadSidewalkHeight(Random));
        }

        private void CreateFootpaths(Prism bounds, ISubdivisionGeometry geometry, INamedDataCollection hierarchicalParameters, string material, float height)
        {
            switch (Vertex.Edges.Count())
            {
                //A junction not connected to any roads... suspicious but not technically disallowed
                case 0:
                //Dead end, don't do anything - the road places everything it needs
                case 1: {
                    break;
                }

                //Connection between 2 roads, this may be one of two things:
                // 1 - A straight on connection, no footpaths needed
                // 2 - A bend, in which case we need to place footpaths
                case 2: {
                    CreateFootpath2(geometry, material, height);

                    break;
                }

                    //N-Way junction
                default: {
                    //todo:
                    break;
                }
            }
        }

        private void CreateFootpath2(ISubdivisionGeometry geometry, string material, float height)
        {
            //A 2 way junction has two parts:
            // - the inner point which we need to place a smooth curve around
            // - the outer part, which we need to lay footpath all the way around

            //Get the roads around this junction
            var r1 = Vertex.Edges.First().BuilderEndingWith(Vertex);
            var r2 = Vertex.Edges.Skip(1).First().BuilderEndingWith(Vertex);

            //Determine the inner point we're curving around
            bool r1LeftInner = r1.LeftEnd.TolerantEquals(r2.RightEnd, 0.01f);

            //If the other points are *also* equal then this is a zero length junction (as mentioned above, straight on) and we have no work to do
            if (r1LeftInner && r1.RightEnd.Equals(r2.LeftEnd))
                return;

            //Create 2 paths (inner and outer)
            CreateFootpath2Inner(geometry, material, height, r1LeftInner, r1, r2);
            CreateFootpath2Outer(geometry, material, height, r1LeftInner, r1, r2);
        }

        private void CreateFootpath2Outer(ISubdivisionGeometry geometry, string material, float height, bool r1LeftInner, IHalfEdgeBuilder r1, IHalfEdgeBuilder r2)
        {
            var points = new List<Vector2>();

            var r1Outer = r1LeftInner ? r1.RightEnd : r1.LeftEnd;
            var r2Outer = r1LeftInner ? r2.LeftEnd : r2.RightEnd;

            //Center of the curve
            var circleCenter = Geometry2D.LineLineIntersection(
                new Line2D(r1Outer, r1.Direction.Perpendicular()),
                new Line2D(r2Outer, r2.Direction.Perpendicular())
            );

            //Sanity check!
            if (!circleCenter.HasValue)
                return;

            //Walk along the outside
            points.AddRange(new CircleSegment {
                CenterPoint = circleCenter.Value.Position,
                StartPoint = r1Outer,
                EndPoint = r2Outer
            }.Evaluate(0.05f));
            points.Add(circleCenter.Value.Position);

            //Create shape covering entire segment
            var shape = geometry.CreatePrism(material, points.Quickhull2D().ToArray(), height);

            //walk backwards along inside (outside offset inwards by pavement width)
            points.Clear();
            points.AddRange(new CircleSegment
            {
                CenterPoint = circleCenter.Value.Position,
                StartPoint = r1Outer + Vector2.Normalize(circleCenter.Value.Position - r1Outer) * r1.SidewalkWidth,
                EndPoint = r2Outer + Vector2.Normalize(circleCenter.Value.Position - r2Outer) * r2.SidewalkWidth
            }.Evaluate(0.05f));
            points.Add(circleCenter.Value.Position);

            var innerShape = geometry.CreatePrism(material, points.Quickhull2D().ToArray(), height);

            //Subtract off inner part
            shape = shape.Subtract(innerShape);

            //Materialize result
            geometry.Union(shape
                .Translate(new Vector3(0, this.GroundOffset(height), 0))
                .Transform(InverseWorldTransformation)
            );
        }

        private void CreateFootpath2Inner(ISubdivisionGeometry geometry, string material, float height, bool r1LeftInner, IHalfEdgeBuilder r1, IHalfEdgeBuilder r2)
        {
            var innerPoint = r1LeftInner ? r1.LeftEnd : r1.RightEnd;
            var r1Outer = r1LeftInner ? r1.RightEnd : r1.LeftEnd;
            var r2Outer = r1LeftInner ? r2.LeftEnd : r2.RightEnd;

            var points = new List<Vector2> {
                innerPoint
            };

            //Points where the footpath terminates at the end of the road
            var r1Point = innerPoint + Vector2.Normalize(r1Outer - innerPoint) * r1.SidewalkWidth;
            var r2Point = innerPoint + Vector2.Normalize(r2Outer - innerPoint) * r2.SidewalkWidth;
            
            //Point to turn the connect around
            var centerPoint = Geometry2D.LineLineIntersection(
                new Line2D(r1Point, r1.Direction.Perpendicular()),
                new Line2D(r2Point, r2.Direction.Perpendicular())
            );

            //Sanity check!
            if (!centerPoint.HasValue)
                return;

            //Evaluate segment
            var curve = new CircleSegment {
                CenterPoint = centerPoint.Value.Position,
                StartPoint = innerPoint + Vector2.Normalize(r1Outer - innerPoint) * r1.SidewalkWidth,
                EndPoint = innerPoint + Vector2.Normalize(r2Outer - innerPoint) * r2.SidewalkWidth,
            };
            points.AddRange(curve.Evaluate(0.05f));

            //Materialize result
            geometry.Union(geometry
                .CreatePrism(material, points.Quickhull2D().ToArray(), height)
                .Translate(new Vector3(0, this.GroundOffset(height), 0))
                .Transform(InverseWorldTransformation)
            );
        }
    }
}
