using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Base_CityGeneration.Datastructures;
using EpimetheusPlugins.Extensions;
using EpimetheusPlugins.Procedural.Utilities;
using HandyCollections.Geometry;
using SwizzleMyVectors;
using SwizzleMyVectors.Geometry;
using MathHelper = Microsoft.Xna.Framework.MathHelper;

namespace Base_CityGeneration.Utilities
{
    /// <summary>
    /// Base class for regions of space defined by a polygon
    /// </summary>
    public abstract class BasePolygonRegion<TSelf, TSection>
        where TSelf : BasePolygonRegion<TSelf, TSection>
        where TSection : class, BasePolygonRegion<TSelf, TSection>.Side.ISection
    {
        #region fields and properties
        private readonly IReadOnlyList<Side> _shape;
        /// <summary>
        /// The shape of this region
        /// </summary>
        public IReadOnlyList<Side> Shape
        {
            get { return _shape; }
        }

        /// <summary>
        /// The points which form the polgon around this region
        /// </summary>
        public IEnumerable<Vector2> Points
        {
            get { return Shape.Select(a => a.Start); }
        }

        private readonly BoundingRectangle _bounds;
        /// <summary>
        /// The AABB of this region
        /// </summary>
        public BoundingRectangle Bounds
        {
            get { return _bounds; }
        }

        private readonly OABR _oabr;
        /// <summary>
        /// The best fit OABB of this region
        /// </summary>
        public OABR OABR
        {
            get { return _oabr; }
        }

        public float Area { get; private set; }

        private float OabrAreaError { get; set; }
        #endregion

        #region construction
        protected BasePolygonRegion(IReadOnlyList<Side> shape, OABR oabr)
        {
            _shape = shape;

            _bounds = BoundingRectangle.CreateFromPoints(Points);
            _oabr = oabr;

            Area = Points.Area();
            OabrAreaError = _oabr.Area - Area;
        }

        protected BasePolygonRegion(IReadOnlyList<Side> shape)
            : this(shape, OABR.Fit(shape.Select(a => a.Start)))
        {
        }

        protected abstract TSelf Construct(IReadOnlyList<Side> shape);
        #endregion

        #region slicing
        internal IEnumerable<TSelf> Slice(Ray2 sliceLine)
        {
            //Basic polygon slicing, now we need to re-establish Side information for these polygon
            var sliced = Points.SlicePolygon(sliceLine);

            //spatially index sides so we can find applicable edges faster later
            var sides = new Quadtree<Side>(Bounds, 4);
            foreach (var side in _shape)
                sides.Insert(new BoundingRectangle(Vector2.Min(side.Start, side.End) - new Vector2(0.01f), Vector2.Max(side.Start, side.End) + new Vector2(0.01f)), side);

            //Construct a region for each part of the result
            var internalSides = new List<KeyValuePair<TSelf, Side>>();
            var regions = (from polygon in sliced select ConstructFromSlicePart(polygon, sides, sliceLine, internalSides)).ToArray();

            if (internalSides.Count % 2 != 0)
                throw new InvalidOperationException("Uneven number of internal sides");

            //Fixup neighbour sections
            while (internalSides.Count > 0)
            {
                //Find the other half of this neighbour relationship
                int j;
                for (j = 0; j < internalSides.Count; j++)
                {
                    if (internalSides[0].Value.Start == internalSides[j].Value.End && internalSides[0].Value.End == internalSides[j].Value.Start)
                        break;
                }

                var ab = internalSides[0];
                var ba = internalSides[j];

                ab.Value.Sections = new[] { ConstructNeighbourSection(ba.Key) };
                ba.Value.Sections = new[] { ConstructNeighbourSection(ab.Key) };

                internalSides.RemoveAt(j);
                internalSides.RemoveAt(0);
            }

            return regions;
        }

        protected abstract TSection ConstructNeighbourSection(TSelf neighbour);

        private TSelf ConstructFromSlicePart(IReadOnlyList<Vector2> polygon, Quadtree<Side> inputSidesQuad, Ray2 sliceLine, List<KeyValuePair<TSelf, Side>> outInternalSides)
        {
            var outputSides = new List<Side>();
            var internalSides = new List<Side>();

            for (var i = 0; i < polygon.Count; i++)
            {
                var a = polygon[i];
                var b = polygon[(i + 1) % polygon.Count];
                var ab = new LineSegment2(a, b).LongLine;

                //Find all sides from the input which overlap this line. Slightly expand the box to handle a perfectly flat line (which would create an empty box and select nothing)
                var candidates = inputSidesQuad
                    .Intersects(new BoundingRectangle(Vector2.Min(a, b) - new Vector2(0.01f), Vector2.Max(a, b) + new Vector2(0.01f)))
                    //.Where(c => c.Start == a || c.End == b)
                    ;
                //There are three types of edge in the sliced shapes:
                // - unchanged edges. start and end in the same place as an existing side
                // - sliced edges. start or end at the same place as an existing edge and co-linear with it
                // - dividing edge. co-linear with slice line

                var unchanged = candidates.SingleOrDefault(c => c.Start.Equals(a) && c.End.Equals(b));
                if (unchanged != null)
                {
                    //Side completely unchanged, simply copy across the data
                    outputSides.Add(new Side(a, b, unchanged.Sections));
                    continue;
                }

                var slicedStart = candidates.SingleOrDefault(c => c.Start.Equals(a) && new LineSegment2(c.Start, c.End).LongLine.Parallelism(ab) == Parallelism.Collinear && new LineSegment2(c.Start, c.End).DistanceToPoint(b) <= 0.01f);
                if (slicedStart != null)
                {
                    //Side overlapping at start, select the correct sections from OverlapPoint -> 0 (Start)
                    outputSides.Add(new Side(a, b, SelectSections(slicedStart, a, b)));
                    continue;
                }

                var slicedEnd = candidates.SingleOrDefault(c => c.End.Equals(b) && new LineSegment2(c.Start, c.End).LongLine.Parallelism(ab) == Parallelism.Collinear && new LineSegment2(c.Start, c.End).DistanceToPoint(a) <= 0.01f);
                if (slicedEnd != null)
                {
                    //Side overlapping at end, select the correct sections from OverlapPoint -> 1 (End)
                    outputSides.Add(new Side(a, b, SelectSections(slicedEnd, a, b)));
                    continue;
                }

                if (sliceLine.Parallelism(ab) == Parallelism.Collinear)
                {
                    //this is the slice line, need to add a neighbour section
                    //But which section is the neighbour? we can't know yet because not all sections exist yet!
                    //Create a side with a null section, we'll fix this up later
                    var s = new Side(a, b, null);
                    internalSides.Add(s);
                    outputSides.Add(s);
                    continue;
                }

                throw new InvalidOperationException("Failed to match up sides when slicing polygon region");
            }

            var region = Construct(outputSides);
            outInternalSides.AddRange(internalSides.Select(a => new KeyValuePair<TSelf, Side>(region, a)));
            return region;
        }

        private IReadOnlyList<TSection> SelectSections(Side side, Vector2 startPoint, Vector2 endPoint)
        {
            var tStart = new LineSegment2(side.Start, side.End).LongLine.ClosestPointDistanceAlongLine(startPoint);
            var tEnd = new LineSegment2(side.Start, side.End).LongLine.ClosestPointDistanceAlongLine(endPoint);

            return SelectSections(side, tStart, tEnd);
        }

        private IReadOnlyList<TSection> SelectSections(Side side, float tStart, float tEnd)
        {
            //Ensure tStart < tEnd
            var tmp = tStart;
            tStart = Math.Min(tStart, tEnd);
            tEnd = Math.Max(tmp, tEnd);

            var sections = new List<TSection>();
            foreach (var section in side.Sections)
            {
                if (section.Start < tEnd && section.End > tStart)
                {
                    //Does the section *entirely* overlap this new section?
                    //If so just copy it over
                    TSection s;
                    if (section.Start >= tStart && section.End <= tEnd)
                        s = section;
                    else
                        s = Subsection(section, tStart, tEnd);

                    //tStart and tEnd are the start and end points of the new side we're creating
                    //They're specific in the range of 0->1 indicating distance along *parent* edge
                    //We need to remap subsections into the range of the new edge
                    sections.Add(Subsection(s, s.Start - tStart, (s.End - s.Start) / (tEnd - tStart)));
                }
            }

            return sections;
        }

        /// <summary>
        /// Cut a subsection out of a section
        /// </summary>
        /// <param name="section">The section to cut a part out of</param>
        /// <param name="tStart">The start of the subsection (0-1 specifying distance *along edge*, not along section)</param>
        /// <param name="tEnd">The end of the subsection (0-1 specifying distance *along edge*, not along section)</param>
        /// <returns></returns>
        protected abstract TSection Subsection(TSection section, float tStart, float tEnd);
        #endregion

        #region error reduction
        /// <summary>
        /// Reduce error until it is below tolerance (recursively for all results)
        /// </summary>
        /// <param name="tolerance"></param>
        /// <returns></returns>
        public IEnumerable<TSelf> RecursiveReduceError(float tolerance)
        {
            var result = new List<TSelf>();

            if (ReduceError(tolerance, result))
            {
                //This shape was reduced, recurse to reduce error of the results
                return (from shape in result
                        from reduced in shape.RecursiveReduceError(tolerance)
                        select reduced).ToArray();
            }
            else
            {
                //shape was unchanged, base case - just return
                return result;
            }
        }

        /// <summary>
        /// Subdivide this region to reduce the areal difference between the OABR and the shape
        /// </summary>
        /// <param name="tolerance">If error is below this tolerance then nothing will happen</param>
        /// <returns></returns>
        public IEnumerable<TSelf> ReduceError(float tolerance)
        {
            var result = new List<TSelf>();
            ReduceError(tolerance, result);
            return result;
        }

        private bool ReduceError(float tolerance, List<TSelf> result)
        {
            //Base case: error is acceptable
            if (OabrAreaError <= tolerance)
            {
                result.Add((TSelf)this);
                return false;
            }

            //There are different split strategies for convex and concave regions (notes here: https://onedrive.live.com/view.aspx?cid=a09c9578613251a0&id=documents&resid=A09C9578613251A0%2159568&app=OneNote&authkey=!AEwuTix9nt7VfKU&&wd=target%28%2F%2FDated%20notes.one%7Ce28ecbf1-86ee-4c11-9c60-b67ed104b40a%2F2015-12-08%7C7ddfd868-4b67-417b-ba7a-c46b46e504fe%2F%29)


            //Find the most concave corner
            var index = -1;
            var turn = float.PositiveInfinity;
            for (var i = 0; i < _shape.Count; i++)
            {
                //Get three points forming a corner
                var a = _shape[i].Start;
                var b = _shape[(i + 1) % _shape.Count].Start;
                var c = _shape[(i + 2) % _shape.Count].Start;

                //Get vectors into and out of corner
                var ab = b - a;
                var bc = c - b;

                //Cross product to determine if this is an interior or exterior turn
                var cross = ab.Cross(bc);

                //If it's not a concave turn we don't care
                if (Math.Sign(cross) <= 0)
                    continue;

                //Calculate the angle of the concave turn
                var angle = MathHelper.Pi - (float)Math.Acos(Vector2.Dot(ab, bc));

                //Do we beat the best yet?
                if (angle < turn)
                {
                    turn = angle;
                    index = i;
                }
            }

            //Check if this is a concave or convex shape
            if (index == -1)
            {
                //Concave (no convex corners found) - Slice down the center of the short axis
                result.AddRange(Slice(new Ray2(_oabr.Middle, _oabr.SplitDirection())));
            }
            else
            {
                //Slice this shape along the most concave split line

                //Get three points forming the corner
                var a = _shape[index].Start;
                var b = _shape[(index + 1) % _shape.Count].Start;
                var c = _shape[(index + 2) % _shape.Count].Start;

                //Get vectors into and out of corner
                var ab = b - a;
                var bc = c - b;

                //Slice along both the lines
                var slicedAB = Slice(new Ray2(b, ab));
                var slicedBC = Slice(new Ray2(b, bc));

                ////Prefer the slice which produces the minimum OABB error
                ////Select the largest error in each set
                //var abMin = slicedAB.Select(x => x.OabrAreaError).Max();
                //var bcMin = slicedBC.Select(x => x.OabrAreaError).Max();
                //var best = abMin <= bcMin ? slicedAB : slicedBC;

                //Prefer the slice which produces the largest smallest region
                var abMin = slicedAB.Select(x => x.Area).Min();
                var bcMin = slicedBC.Select(x => x.Area).Min();
                var best = abMin <= bcMin ? slicedBC : slicedAB;

                //Output the set with the smallest largest error
                result.AddRange(best);
            }

            return true;
        }
        #endregion

        /// <summary>
        /// A single edge of a region
        /// </summary>
        public class Side
        {
            public Vector2 Start { get; private set; }
            public Vector2 End { get; private set; }

            public IReadOnlyList<TSection> Sections { get; internal set; }

            public Side(Vector2 start, Vector2 end, IReadOnlyList<TSection> sections)
            {
                Sections = sections;

                End = end;
                Start = start;
            }

            /// <summary>
            /// A section of a side
            /// </summary>
            public interface ISection
            {
                /// <summary>
                /// the start point of this section (distance along side in units of side length)
                /// </summary>
                float Start { get; }

                /// <summary>
                /// the end point of this section (distance along side in units of side length)
                /// </summary>
                float End { get; }
            }
        }
    }
}
