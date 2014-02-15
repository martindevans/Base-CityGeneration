using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Myre.Extensions;
using Placeholder.ConstructiveSolidGeometry;
using Placeholder.ConstructiveSolidGeometry.Clipping2D;

namespace Base_CityGeneration.Elements.Block.Parcelling
{
    /// <summary>
    /// Parcels an area by recursively splitting the area along the middle of the OBB of the area
    /// </summary>
    public class ObbParceller
        :IParceller
    {
        private readonly Func<double> _random;

        public float RoadAccessChance { get; set; }
        public float NonOptimalOabbChance { get; set; }
        public float NonOptimalOabbMaxRatio { get; set; }

        private readonly List<ITerminationRule> _terminators = new List<ITerminationRule>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="random"></param>
        /// <param name="roadAccessChance">The chance that a plot will have road access</param>
        /// <param name="nonOptimalOabbChance">The chance that an Oabb other than the smallest will be selected</param>
        /// <param name="nonOptimalOabbMaxRatio">The maximum ratio between optimal Oabb and selected Oabb</param>
        public ObbParceller(Func<double> random, float roadAccessChance, float nonOptimalOabbChance = 0, float nonOptimalOabbMaxRatio = 1)
        {
            _random = random;
            RoadAccessChance = roadAccessChance;
            NonOptimalOabbChance = nonOptimalOabbChance;
            NonOptimalOabbMaxRatio = nonOptimalOabbMaxRatio;
        }

        public void AddTerminationRule(ITerminationRule rule)
        {
            _terminators.Add(rule);
        }

        public IEnumerable<Parcel> GenerateParcels(Parcel root)
        {
            return RecursiveSplit(root);
        }

        private IEnumerable<Parcel> RecursiveSplit(Parcel parcel)
        {
            //Accumulate chance of termination, checking for any rule which forbods it (i.e. probability zero)
            float accumulator = 0;
            bool noChance = false;
            for (int i = 0; i < _terminators.Count && !noChance; i++)
            {
                var c = _terminators[i].TerminationChance(parcel);
                accumulator += c;
                noChance |= c <= 0;
            }

            //If random chance beats average of all termination chances then stop here
            if (accumulator / _terminators.Count >= _random())
                yield return parcel;

            OABB oabb = FitOabb(parcel, NonOptimalOabbChance, NonOptimalOabbMaxRatio);

            var splitLine = oabb.SplitDirection();
            var children = Split(parcel, splitLine, oabb.Middle).ToArray();

            //If this split created children with no road access then flip the split line by 90 and try again
            if (children.Any(c => !c.HasRoadAccess() && _random() <= RoadAccessChance))
            {
                splitLine = splitLine.Perpendicular();
                children = Split(parcel, splitLine, oabb.Middle).ToArray();
            }

            if (_terminators.Any(t => children.Any(t.Discard)))
                yield return parcel;
            else
                foreach (var child in children.SelectMany(RecursiveSplit))
                    yield return child;
        }

        private IEnumerable<Parcel> Split(Parcel parcel, Vector2 direction, Vector2 point)
        {
            List<IntPoint> polygon = new List<IntPoint>(parcel.Edges.Select(e => e.Start).Select(p => new IntPoint((int)(p.X * 1000), (int)(p.Y * 1000))));
            List<IntPoint> clip = new List<IntPoint>(new[]
            {
                point + direction * 50000,
                point + direction * 50000 + direction.Perpendicular() * 50000,
                point - direction * 50000 + direction.Perpendicular() * 50000,
                point - direction * 50000
            }.Select(p => new IntPoint((int) (p.X * 1000), (int) (p.Y * 1000))));

            var c = new Clipper();
            c.AddPolygon(polygon, PolyType.Subject);
            c.AddPolygon(clip, PolyType.Clip);

            //Clipper cannot directly cut polygon, instead we've formed a really massive rectangle covering one side of the split line and we shall perform difference (left) and intersection (right)

            List<List<IntPoint>> difference = new List<List<IntPoint>>();
            c.Execute(ClipType.Difference, difference, PolyFillType.EvenOdd, PolyFillType.EvenOdd);

            List<List<IntPoint>> intersection = new List<List<IntPoint>>();
            c.Execute(ClipType.Intersection, intersection, PolyFillType.EvenOdd, PolyFillType.EvenOdd);

            foreach (var child in difference)
                yield return ToParcel(parcel, child);
            foreach (var child in intersection)
                yield return ToParcel(parcel, child);
        }

        private static Parcel ToParcel(Parcel parent, IEnumerable<IntPoint> child)
        {
            Vector2[] points = child.Select(i => new Vector2(i.X / 1000f, i.Y / 1000f)).ToArray();

            //Build edges from this set of points
            Parcel.Edge[] edges = new Parcel.Edge[points.Length];
            for (int i = 0; i < points.Length; i++)
                edges[i] = new Parcel.Edge { Start = points[i], End = points[(i + 1) % points.Length], HasRoadAccess = false };

            //Find egdes in parent which are coincident with edges of child
            //Copy road accessibility across
            foreach (var parentEdge in parent.Edges)
            {
                var parentDirection = Vector2.Normalize(parentEdge.End - parentEdge.Start);
                for (int j = 0; j < edges.Length; j++)
                {
                    var childEdge = edges[j];

                    var s = Math.Abs(ConvexHullExtensions.DistanceFromPointToLine(childEdge.Start, parentEdge.Start, parentDirection));
                    var e = Math.Abs(ConvexHullExtensions.DistanceFromPointToLine(childEdge.End, parentEdge.Start, parentDirection));

                    if (s < 0.1 && e < 0.1) //Pretty massive threshold, but it's only really 10cm, so close enough when we're talking about entire city blocks!
                    {
                        edges[j].HasRoadAccess = parentEdge.HasRoadAccess;
                        break;
                    }
                }
            }

            return new Parcel(edges);
        }

        private OABB FitOabb(Parcel parcel, float nonOptimalityChance, float maximumNonOptimality)
        {
            //Finding the OABB of the hull is the same as finding the OABB of the parcel, but is quicker
            var hull = parcel.Points().Quickhull2D().ToArray();

            //Find middle of hull
            Vector2 middle = hull.Aggregate(Vector2.Zero, (current, t) => current + t / hull.Length);

            //Generate ordered list of all OABBs (each aligned with an edge of the convex hull)
            var oabbs = Enumerable
                .Range(0, hull.Length)
                .Select(i =>
                {
                    var a = hull[i];
                    var b = hull[(i + 1) % hull.Length];

                    //Get the angle of this edge from the vertical
                    var angle = (float) Math.Atan2(b.X - a.X, b.Y - a.Y);

                    //Find the bounding box for this orientation
                    Vector2 min = new Vector2(float.MaxValue);
                    Vector2 max = new Vector2(float.MinValue);
                    foreach (var rotated in hull.Select(v => RotateAround(v, middle, -angle)))
                    {
                        min.X = Math.Min(min.X, rotated.X);
                        min.Y = Math.Min(min.Y, rotated.Y);
                        max.X = Math.Max(max.X, rotated.X);
                        max.Y = Math.Max(max.Y, rotated.Y);
                    }

                    var extents = (max - min) / 2;
                    return new OABB
                    {
                        Middle = middle,
                        Rotation = -angle,
                        Extents = extents,
                        Area = extents.X * extents.Y * 4
                    };
                })
                .OrderBy(a => a.Extents.X * a.Extents.Y * 4).ToArray();

            //Now select an OABB from this list, with the first (smallest) being the most likely
            int selected = 0;
            for (int i = 1; i < oabbs.Length; i++)
            {
                //Do not allow the area ratio between optimal and selected to go over the non optimality limit
                if (oabbs[i].Area / (oabbs[0]).Area > maximumNonOptimality)
                    break;

                //We're not breaking the non optimality limit, so what's the chance of selecting this?
                var chance = Math.Pow(nonOptimalityChance, i);
                selected = _random() < chance ? i : selected;
            }

            return oabbs[selected];
        }

        private static Vector2 RotateAround(Vector2 point, Vector2 origin, float angle)
        {
            var c = (float)Math.Cos(angle);
            var s = (float)Math.Sin(angle);

            return new Vector2(
                c * (point.X - origin.X) - s * (point.Y - origin.Y) + origin.X,
                s * (point.X - origin.X) + c * (point.Y - origin.Y) + origin.Y
            );
        }

        private struct OABB
        {
            public Vector2 Middle;
            public float Rotation;
            public Vector2 Extents;
            public float Area;

            public Vector2 SplitDirection()
            {
                return ((Extents.X < Extents.Y))
                    ? new Vector2((float) Math.Cos(Rotation), (float) Math.Sin(Rotation))
                    : new Vector2((float) Math.Sin(Rotation), (float) Math.Cos(Rotation));
            }
        }
    }
}
