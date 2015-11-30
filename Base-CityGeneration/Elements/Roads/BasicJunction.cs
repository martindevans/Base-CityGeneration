using Base_CityGeneration.Datastructures.HalfEdge;
using Base_CityGeneration.Elements.Generic;
using Base_CityGeneration.Styles;
using EpimetheusPlugins.Extensions;
using EpimetheusPlugins.Procedural;
using EpimetheusPlugins.Procedural.Utilities;
using EpimetheusPlugins.Scripts;
using System.Numerics;
using Myre.Collections;
using Myre.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using SwizzleMyVectors;
using SwizzleMyVectors.Geometry;
using MathHelper = Microsoft.Xna.Framework.MathHelper;

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

            if (r1LeftInner) {
                //One side is an inner side, create a footpath around that point
                CreateFootpath2Inner(geometry, material, height, true, a, b);
            } else {
                //No point is an inner point, follow around the outer edge connecting these two roads
                CreateFootpath2Outer(geometry, material, height, a, b);

            }
        }

        private void CreateFootpath2Outer(ISubdivisionGeometry geometry, string material, float height, IHalfEdgeBuilder r1, IHalfEdgeBuilder r2)
        {
            //r1.LeftEnd and r2.RightEnd are the outsides of the roads
            //Trace a path along the boundary between these points, use the path which does *not* include the other points
            Vector2[] cwp, ccwp;
            bool cw, ccw;
            Bounds.Footprint.TraceConnectingPath(
                Vector3.Transform(r1.LeftEnd.X_Y(0), InverseWorldTransformation).XZ(),
                Vector3.Transform(r2.RightEnd.X_Y(0), InverseWorldTransformation).XZ(),
                0.01f, out cwp, out cw, out ccwp, out ccw,
                Vector3.Transform(r1.RightEnd.X_Y(0), InverseWorldTransformation).XZ(),
                Vector3.Transform(r2.LeftEnd.X_Y(0), InverseWorldTransformation).XZ());

            //Sanity check (only 1 direction should be acceptable)
            if (!(cw ^ ccw))
                return;

            //Follow along edge, then back along edge (pushed inwards by footpath width)
            //todo: Offset width by angle at end of road
            var outerPoints = cw ? cwp : ccwp;
            var innerPoints = new Vector2[cw ? cwp.Length : ccwp.Length];

            float widthStart = cw ? r1.SidewalkWidth : r2.SidewalkWidth;
            float widthEnd = cw ? r2.SidewalkWidth : r1.SidewalkWidth;

            for (int i = 0; i < outerPoints.Length && outerPoints.Length > 1; i++) {
                Vector2 dir;
                float w;
                if (i == 0) {
                    dir = Vector2.Normalize(cw ? r1.RightEnd - r1.LeftEnd : r2.LeftEnd - r2.RightEnd);
                    w = CalculateFootpathAngleScale(cw ? r1.Direction : r2.Direction, dir, widthStart);
                }
                else if (i == outerPoints.Length - 1)
                {
                    dir = Vector2.Normalize(cw ? r2.LeftEnd - r2.RightEnd : r1.RightEnd - r1.LeftEnd);
                    w = CalculateFootpathAngleScale(cw ? r2.Direction : r1.Direction, -dir, widthEnd);
                } else {
                    //perpendicular to path direction
                    dir = Vector2.Normalize(outerPoints[i] - outerPoints[i - 1]).Perpendicular() * (cw ? 1 : -1);
                    //Interpolate widths along path
                    var t = i / (float)(outerPoints.Length - 1);
                    w = MathHelper.Lerp(widthStart, widthEnd, t);
                }

                innerPoints[i] = outerPoints[i] + dir * w;
            }

            //Create footprint from 2 halves
            var footprint = cw ? outerPoints.Append(innerPoints.Reverse()) : outerPoints.Reverse().Append(innerPoints);
            geometry.Union(geometry
                .CreatePrism(material, footprint.ToArray(), height)
                .Translate(new Vector3(0, this.GroundOffset(height), 0))
            );
        }

        private float CalculateFootpathAngleScale(Vector2 along, Vector2 across, float w)
        {
            return w / Vector2.Dot(along.Perpendicular(), across);
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
            var centerPoint = new Ray2(r1Point, r1.Direction.Perpendicular()).Intersects(new Ray2(r2Point, r2.Direction.Perpendicular()));

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
                .CreatePrism(material, footprint.ConvexHull().ToArray(), height)
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
