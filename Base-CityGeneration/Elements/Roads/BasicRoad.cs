﻿using System;
using System.Collections.Generic;
using System.Linq;
using Base_CityGeneration.Datastructures.HalfEdge;
using Base_CityGeneration.Elements.Generic;
using EpimetheusPlugins.Procedural;
using EpimetheusPlugins.Procedural.Utilities;
using EpimetheusPlugins.Scripts;
using Microsoft.Xna.Framework;
using Myre.Collections;
using Myre.Extensions;

namespace Base_CityGeneration.Elements.Roads
{
    [Script("0C714177-06D9-4E20-8028-FA0CE6519892", "Basic Road")]
    public class BasicRoad
        : ProceduralScript, IRoad
    {
        public float GroundHeight { get; set; }

        public HalfEdge<IVertexBuilder, IHalfEdgeBuilder, IFaceBuilder> HalfEdge { get; set; }

        public float Width { get; set; }

        public override bool Accept(Prism bounds, INamedDataProvider parameters)
        {
            return true;
        }

        public override void Subdivide(Prism bounds, ISubdivisionGeometry geometry, INamedDataCollection hierarchicalParameters)
        {
            //Create a flat plane filling the bounds of this area
            this.CreateFlatPlane(geometry, "tarmac", bounds.Footprint, 1, -1);

            //Add in footpaths
            CreateFootpaths(bounds, geometry, hierarchicalParameters, "concrete", 0.15f);
        }

        private void CreateFootpaths(Prism bounds, ISubdivisionGeometry geometry, INamedDataCollection hierarchicalParameters, string material, float height)
        {
            //Get a builder for this edge
            var builder = HalfEdge.BuilderEndingWith(HalfEdge.EndVertex);

            //Extract sections of sidewalk width into this shape
            var sections = bounds.Footprint.Sections(builder.SidewalkWidth).ToArray();

            //builder data is in world space, transform it back to node-local space
            //the transform is in 3D space, extend out vector2s up to 3D space (X,0,Y) then transform then come back to v2 space (XZ)
            var leftEnd = Vector3.Transform(builder.LeftEnd.X_Y(0), InverseWorldTransformation).XZ();
            var rightEnd = Vector3.Transform(builder.RightEnd.X_Y(0), InverseWorldTransformation).XZ();
            var leftStart = Vector3.Transform(builder.LeftStart.X_Y(0), InverseWorldTransformation).XZ();
            var rightStart = Vector3.Transform(builder.RightStart.X_Y(0), InverseWorldTransformation).XZ();

            //Create geometry for edge sections
            MaterializeEdgeSections(geometry, material, height, sections, builder, leftEnd, rightEnd, leftStart, rightStart);

            //Create geometry for corner sections
            MaterializeCornerSections(geometry, material, height, sections, builder, rightEnd, leftEnd, leftStart, rightStart);
        }

        private void MaterializeCornerSections(ISubdivisionGeometry geometry, string material, float height, IEnumerable<Walls.Section> sections, IHalfEdgeBuilder builder, Vector2 rightEnd, Vector2 leftEnd, Vector2 leftStart, Vector2 rightStart)
        {
            foreach (var section in sections)
            {
                //Skip non corner sections
                if (!section.IsCorner)
                {
                    continue;
                }

                //which vertex is this corner bordering onto?
                Vector2 endPointA;
                Vector2 endPointB;
                var vertex = DetermineBorderingVertex(section, builder, rightEnd, leftEnd, leftStart, rightStart, out endPointA, out endPointB);

                //Determine what to do based on junction type
                var count = vertex.Edges.Count();
                switch (count)
                {
                    case 0: {
                        throw new InvalidOperationException("Junction has 0 connecting roads");
                    }

                    //Dead end, place the corners
                    case 1: {
                        MaterializeSection(geometry, material, section, height);
                        break;
                    }

                    //2-way junction (connect)
                    case 2: {
                        //End line of road segment
                        var lAcross = new Line2D(endPointA, Vector2.Normalize(endPointB - endPointA));

                        //Direction along footpath
                        var cDist = Geometry2D.DistanceFromPointToLine(section.C, lAcross);
                        var aDist = Geometry2D.DistanceFromPointToLine(section.A, lAcross);
                        Vector2 alongFootpath = aDist < cDist ? Vector2.Normalize(section.B - section.C) : Vector2.Normalize(section.B - section.A);

                        //Inner line of footpath
                        var lInner = new Line2D(section.D, alongFootpath);

                        //Intersection point of the two
                        var point = Geometry2D.LineLineIntersection(lInner, lAcross);

                        //Sanity check
                        if (!point.HasValue)
                            continue;

                        Vector2[] shape = new Vector2[4] {
                            section.B,
                            section.D,
                            point.Value.Position,
                            aDist < cDist ? section.C : section.A
                        };
                        MaterializeSection(geometry, material, shape, height);
                        break;
                    }

                    //N-Way junction (connect)
                    default: {
                        break;
                    }
                }
            }
        }

        private static Vertex<IVertexBuilder, IHalfEdgeBuilder, IFaceBuilder> DetermineBorderingVertex(Walls.Section section, IHalfEdgeBuilder builder, Vector2 rightEnd, Vector2 leftEnd, Vector2 leftStart, Vector2 rightStart, out Vector2 endPointA, out Vector2 endPointB)
        {
            //Point B is the corner point, which of the corners is it?
            Vertex<IVertexBuilder, IHalfEdgeBuilder, IFaceBuilder> vertex;

            if (Vector2.DistanceSquared(section.B, rightEnd) < 0.01f)
            {
                vertex = builder.HalfEdge.EndVertex;
                endPointA = leftEnd;
                endPointB = rightEnd;
            }
            else if (Vector2.DistanceSquared(section.B, leftEnd) < 0.01f)
            {
                vertex = builder.HalfEdge.EndVertex;
                endPointA = leftEnd;
                endPointB = rightEnd;
            }
            else if (Vector2.DistanceSquared(section.B, rightStart) < 0.01f)
            {
                vertex = builder.HalfEdge.Pair.EndVertex;
                endPointA = leftStart;
                endPointB = rightStart;
            }
            else if (Vector2.DistanceSquared(section.B, leftStart) < 0.01f)
            {
                vertex = builder.HalfEdge.Pair.EndVertex;
                endPointA = leftStart;
                endPointB = rightStart;
            }
            else
            {
                throw new InvalidOperationException("Footpath corner is not in any of the corners of the road section");
            }

            return vertex;
        }

        private void MaterializeEdgeSections(ISubdivisionGeometry geometry, string material, float height, IEnumerable<Walls.Section> sections, IHalfEdgeBuilder builder, Vector2 leftEnd, Vector2 rightEnd, Vector2 leftStart, Vector2 rightStart)
        {
            //Cache these checks
            var deadEnd = builder.HalfEdge.EndVertex.Edges.Count() == 1;
            var deadStart = builder.HalfEdge.Pair.EndVertex.Edges.Count() == 1;

            foreach (var section in sections)
            {
                //Skip over all corner sections
                if (section.IsCorner)
                    continue;

                //If this footpath is along the side of the road
                //If this footpath is along an end, and this end is a dead end (junction has one road)
                if (Math.Abs(Vector2.Dot(section.Along, builder.Direction)) > 0.99f ||
                    (deadEnd && IsAlongLineSegment(section, new LineSegment2D(leftEnd, rightEnd))) ||
                    (deadStart && IsAlongLineSegment(section, new LineSegment2D(leftStart, rightStart)))
                )
                {
                    MaterializeSection(geometry, material, section, height);
                }
            }
        }

        private static bool IsAlongLineSegment(Walls.Section section, LineSegment2D segment)
        {
            //Check if this section points in the same (or opposite) direction
            if (!(Math.Abs(Vector2.Dot(section.Along, segment.Line().Direction)) > 0.99f))
                return false;

            //Check if the section is actually on the line
            //C and D are the outside points of the section, so they should lie on the line at the edge of the road
            if (Geometry2D.DistanceFromPointToLineSegment(section.C, segment) > 0.01f)
                return false;
            if (Geometry2D.DistanceFromPointToLineSegment(section.D, segment) > 0.01f)
                return false;

            return true;
        }

        private void MaterializeSection(ISubdivisionGeometry geometry, string material, Walls.Section section, float height)
        {
            MaterializeSection(geometry, material, new Vector2[] {section.A, section.B, section.C, section.D}, height);
        }

        private void MaterializeSection(ISubdivisionGeometry geometry, string material, IEnumerable<Vector2> shape, float height)
        {
            this.CreateFlatPlane(geometry, material, shape.Quickhull2D().ToArray(), height);
        }
    }
}
