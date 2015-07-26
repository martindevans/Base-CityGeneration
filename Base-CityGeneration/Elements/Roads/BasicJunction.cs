using System;
using System.Linq;
using Base_CityGeneration.Datastructures.HalfEdge;
using Base_CityGeneration.Elements.Generic;
using EpimetheusPlugins.Extensions;
using EpimetheusPlugins.Procedural;
using EpimetheusPlugins.Scripts;
using Microsoft.Xna.Framework;
using Myre.Collections;
using Myre.Extensions;

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

            CreateFootpaths(bounds, geometry, hierarchicalParameters, "grass");
        }

        private void CreateFootpaths(Prism bounds, ISubdivisionGeometry geometry, INamedDataCollection hierarchicalParameters, string material)
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
                    //A 2 way junction has two parts:
                    // - the inner point which we need to place a smooth curve around
                    // - the outer part, which we need to lay footpath all the way around

                    //Get the roads around this junction
                    var r1 = Vertex.Edges.First().BuilderEndingWith(Vertex);
                    var r2 = Vertex.Edges.Skip(1).First().BuilderEndingWith(Vertex);

                    //Determine the inner point we're curving around
                    bool r1LeftInner = r1.LeftEnd.TolerantEquals(r2.RightEnd, 0.01f);
                    var innerPoint = r1LeftInner ? r1.LeftEnd : r1.RightEnd;

                    //If the other points are *also* equal then this is a zero length junction (as mentioned above, straight on) and we have no work to do
                    if (r1LeftInner && r1.RightEnd.Equals(r2.LeftEnd))
                        break;

                    //Place curve around "innerPoint"
                    //Calculate a matrix to transform a cylinder into a skewed cylinder connecting both footpaths
                    var f = (Vector2.Normalize(r1LeftInner ? (r2.LeftEnd - innerPoint) : (r2.RightEnd - innerPoint))).X_Y(0);
                    var r = (Vector2.Normalize(r1LeftInner ? (r1.RightEnd - innerPoint) : (r1.LeftEnd - innerPoint))).X_Y(0);
                    var roadInnerAngle = Math.Acos(Vector3.Dot(r, f));
                    var footpathRoadEndAngle = MathHelper.PiOver2 - roadInnerAngle;
                    var t = 1f / (float)Math.Cos(footpathRoadEndAngle);
                    r *= -Math.Sign(Vector3.Cross(f, r).Y);

                    var matrix = Matrix.Identity;
                    matrix.Right = r * t * r2.SidewalkWidth;
                    matrix.Forward = f * t * r1.SidewalkWidth;

                    var innerPointLocalSpace = Vector3.Transform(innerPoint.X_Y(0), InverseWorldTransformation);

                    geometry.Union(geometry.CreateCylinder("grass", 40)
                        .Transform(matrix)
                        .Translate(innerPointLocalSpace)
                    );

                    //todo:
                    break;
                }

                //N-Way junction
                default: {
                    //todo:
                    break;
                }
            }
        }
    }
}
