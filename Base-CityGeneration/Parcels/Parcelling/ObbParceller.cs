using System;
using System.Collections.Generic;
using System.Linq;
using Base_CityGeneration.Utilities.Numbers;
using EpimetheusPlugins.Procedural.Utilities;
using Microsoft.Xna.Framework;
using Myre.Extensions;
using Poly2Tri.Triangulation.Sets;

namespace Base_CityGeneration.Parcels.Parcelling
{
    /// <summary>
    /// Parcels an area by recursively splitting the area along the middle of the OBB of the area
    /// </summary>
    public class ObbParceller
        :IParceller
    {
        public float NonOptimalOabbChance { get; set; }
        public float NonOptimalOabbMaxRatio { get; set; }

        public BaseValueGenerator SplitPointGenerator { get; set; }

        private readonly List<ITerminationRule> _terminators = new List<ITerminationRule>();

        public ObbParceller()
        {
            SplitPointGenerator = new NormallyDistributedValue(-0.35f, 0.0f, 0.35f, 0.2f, true);
        }

        public void AddTerminationRule(ITerminationRule rule)
        {
            _terminators.Add(rule);
        }

        public IEnumerable<Parcel> GenerateParcels(Parcel root, Func<double> random)
        {
            return RecursiveSplit(root, random);
        }

        private IEnumerable<Parcel> RecursiveSplit(Parcel parcel, Func<double> random)
        {
            //Accumulate chance of termination, checking for any rule which forbods it (i.e. probability zero)
            float accumulator = 0;
            bool noChance = false;
            for (int i = 0; i < _terminators.Count && !noChance; i++)
            {
                var c = _terminators[i].TerminationChance(parcel);
                if (c.HasValue)
                {
                    accumulator += c.Value;
                    noChance |= c.Value <= 0;
                }
            }

            //If random chance beats average of all termination chances then stop here
            if (accumulator / _terminators.Count >= random())
                return new[] { parcel };

            OABB oabb = FitOabb(parcel, NonOptimalOabbChance, NonOptimalOabbMaxRatio, random);

            var splitLine = oabb.SplitDirection();
            var children = Split(parcel, oabb, splitLine, random).ToArray();

            //If any children are discarded try splitting the other way
            // ReSharper disable once AccessToModifiedClosure
            if (_terminators.Any(t => children.Any(c => t.Discard(c, random))))
            {
                splitLine = splitLine.Perpendicular();
                children = Split(parcel, oabb, splitLine, random).ToArray();
            }

            //Either return this parcel because we can't find any valid children, or continue recursive splitting
            if (_terminators.Any(t => children.Any(c => t.Discard(c, random))))
                return new[] { parcel };
            else
                return children.SelectMany(a => RecursiveSplit(a, random)).ToArray();
        }

        #region static helpers
        private IEnumerable<Parcel> Split(Parcel parcel, OABB oabb, Vector2 sliceDirection, Func<double> random)
        {
            var point = oabb.Middle + Math.Max(oabb.Extents.X, oabb.Extents.Y) * sliceDirection.Perpendicular() * SplitPointGenerator.SelectFloatValue(random);

            var slices = parcel.Points().SlicePolygon(new Line2D(point, sliceDirection));

            return slices.Select(a => ToParcel(parcel, a));
        }

        private static Parcel ToParcel(Parcel parent, IEnumerable<Vector2> child)
        {
            Vector2[] points = child.ToArray();
            if (points.Area() < 0)
                Array.Reverse(points);

            //Build edges from this set of points
            Parcel.Edge[] edges = new Parcel.Edge[points.Length];
            for (int i = 0; i < points.Length; i++)
                edges[i] = new Parcel.Edge { Start = points[i], End = points[(i + 1) % points.Length], Resources = new string[0] };

            //Find egdes in parent which are coincident with edges of child
            //Copy road accessibility across
            foreach (var parentEdge in parent.Edges)
            {
                var parentDirection = Vector2.Normalize(parentEdge.End - parentEdge.Start);
                for (int j = 0; j < edges.Length; j++)
                {
                    var childEdge = edges[j];

                    var s = Math.Abs(Geometry2D.DistanceFromPointToLine(childEdge.Start, new Line2D(parentEdge.Start, parentDirection)));
                    var e = Math.Abs(Geometry2D.DistanceFromPointToLine(childEdge.End, new Line2D(parentEdge.Start, parentDirection)));

                    if (s < 0.01 && e < 0.01) //Pretty massive threshold, but it's only really 1cm, so close enough when we're talking about entire city blocks!
                    {
                        edges[j].Resources = parentEdge.Resources;
                        break;
                    }
                }
            }

            return new Parcel(edges, parent);
        }

        internal static OABB FitOabb(Parcel parcel, float nonOptimalityChance, float maximumNonOptimality, Func<double> random)
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
                selected = random() < chance ? i : selected;
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
        #endregion

        internal struct OABB
        {
            public Vector2 Middle;
            public float Rotation;
            public Vector2 Extents;
            public float Area;

            public Vector2 SplitDirection()
            {
                var sin = (float)Math.Sin(Rotation);
                var cos = (float)Math.Cos(Rotation);

                return Vector2.Normalize((Extents.X < Extents.Y) ? new Vector2(cos, sin) : new Vector2(sin, cos));
            }
        }
    }
}
