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
using System.Collections.Generic;
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

                //N-Way junction
                default: {
                    CreateFootpathN(geometry, material, height);
                    break;
                }
            }
        }

        private void CreateFootpathN(ISubdivisionGeometry geometry, string material, float height)
        {
            //Order the edges by their angle around the vertex
            var orderedEdges = Vertex.OrderedEdges().ToArray();

            //Create path for pairs of roads
            for (int i = 0; i < orderedEdges.Length; i++)
            {
                var prev = orderedEdges[(i + orderedEdges.Length - 1) % orderedEdges.Length];
                var edge = orderedEdges[i];

                CreateFootpathFromPair(geometry, material, height, prev, edge);
            }
        }

        private void CreateFootpathFromPair(ISubdivisionGeometry geometry, string material, float height, IHalfEdgeBuilder a, IHalfEdgeBuilder b)
        {
            //Determine the inner point we're curving around
            bool r1LeftInner = a.LeftEnd.TolerantEquals(b.RightEnd, 0.01f);
            bool r1RightInner = a.RightEnd.TolerantEquals(b.LeftEnd, 0.01f);

            //Straight on, no junction and no paths needed
            if (r1LeftInner && r1RightInner)
                return;

            if (r1LeftInner ^ r1RightInner) {
                //One side is an inner side, create a footpath around that point
                CreateFootpath2Inner(geometry, material, height, r1LeftInner, a, b);
            } else {
                //todo: No point is an inner point, follow around the outer edge connecting these two roads

                //CreateFootpath2Outer(geometry, "grass", height, r1LeftInner, a, b);

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

            var points = new List<Vector2> {
                innerPoint
            };

            //Points where the footpath terminates at the end of the road
            var r1Across = r1LeftInner ? r1.Direction.Perpendicular() : -r1.Direction.Perpendicular();
            var r1Point = innerPoint + r1Across * r1.SidewalkWidth;
            var r2Across = r1LeftInner ? -r2.Direction.Perpendicular() : r2.Direction.Perpendicular();
            var r2Point = innerPoint + r2Across * r2.SidewalkWidth;

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
                StartPoint = r1Point,
                EndPoint = r2Point,
            };
            points.AddRange(curve.Evaluate(0.05f));

            //Helpers function for creating prisms
            Action<IEnumerable<Vector2>, bool> unionPrism = (footprint, check) => geometry.Union(geometry
                .CreatePrism(material, footprint.Quickhull2D().ToArray(), height)
                .Translate(new Vector3(0, this.GroundOffset(height), 0))
                .Transform(InverseWorldTransformation),
                check
            );

            //Materialize result
            unionPrism(points, false);

            //Extend straight sections to edge of node
            //We only need to do this if we were turning precisely about the contact points of these two roads
            if (centerPoint.Value.Position.TolerantEquals(innerPoint, 0.1f)) {
                unionPrism(new[] {
                    r1Point,
                    innerPoint,
                    innerPoint - r1.Direction * r1.SidewalkWidth,
                    r1Point - r1.Direction * r1.SidewalkWidth
                }, true);

                unionPrism(new[] {
                    r2Point,
                    innerPoint,
                    innerPoint - r2.Direction * r2.SidewalkWidth,
                    r2Point - r2.Direction * r2.SidewalkWidth
                }, true);
            }
        }
    }
}
