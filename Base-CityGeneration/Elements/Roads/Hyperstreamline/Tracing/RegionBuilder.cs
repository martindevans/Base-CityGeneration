using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Numerics;

namespace Base_CityGeneration.Elements.Roads.Hyperstreamline.Tracing
{
    class RegionBuilder
    {
        private readonly HashSet<Edge> _unprocessed;
        private readonly HashSet<KeyValuePair<bool, Edge>> _halfProcessed = new HashSet<KeyValuePair<bool, Edge>>();

        public RegionBuilder(IEnumerable<Vertex> vertices)
        {
            Contract.Requires(vertices != null, "vertices");

            _unprocessed = new HashSet<Edge>(vertices.SelectMany(v => v.Edges));
        }

        public IEnumerable<Region> Regions()
        {
            Contract.Ensures(Contract.Result<IEnumerable<Region>>() != null);

            var regions = new List<Region>();

            //Empty out these two collections (it's larger, let's keep it as small as possible)
            while (_unprocessed.Count > 0 || _halfProcessed.Count > 0)
            {
                Edge edge;
                bool direction;
                if (_halfProcessed.Count > 0)
                {
                    var e = _halfProcessed.First();
                    edge = e.Value;
                    direction = e.Key;
                }
                else
                {
                    edge = _unprocessed.First();
                    direction = true;
                }

                var boundary = WalkRegionBoundary(edge, direction, _unprocessed, _halfProcessed);
                if (boundary != null)
                    regions.Add(boundary);
            }

            //Sort the regions into a consistent order
            //Even small variations in min and max should result in exactly the same order each time
            return regions.OrderBy(a => Math.Round(a.Min.X))
                          .ThenBy(a => Math.Round(a.Min.Y))
                          .ThenBy(a => Math.Round(a.Max.X))
                          .ThenBy(a => Math.Round(a.Max.Y));
        }

        private static Region WalkRegionBoundary(Edge start, bool direction, ISet<Edge> unprocessed, ISet<KeyValuePair<bool, Edge>> halfProcessed)
        {
            //We only want to trace boundary streamlines (width zero) on the inside, not the outside
            //Skip this streamline if it is a clockwise boundary streamline (and update collections)
            if (start.Streamline.Width == 0)
            {
                halfProcessed.Remove(new KeyValuePair<bool, Edge>(true, start));
                halfProcessed.Remove(new KeyValuePair<bool, Edge>(false, start));
                unprocessed.Remove(start);
                return null;
            }

            var points = new List<Vertex>();

            var e = start;
            do
            {
                //If this edge was previously unprocessed, the other side has not been touched yet, queue it up to be processed
                if (unprocessed.Remove(e))
                    halfProcessed.Add(new KeyValuePair<bool, Edge>(!direction, e));
                else if (!halfProcessed.Remove(new KeyValuePair<bool, Edge>(direction, e)))
                    return null;

                //Accumulate edges
                var p = (direction ? e.B : e.A);
                points.Add(p);

                //Step to next edge
                e = WalkNextEdge(e, ref direction);

                if (e == null)
                    return null;

            } while (e != start);

            RemoveDeadEnds(points);

            if (points.Count >= 3)
                return new Region(points.Select(a => a.Position).ToList());

            return null;
        }

        private static void RemoveDeadEnds(List<Vertex> points)
        {
            //it's possible for a region to follow up a dead end road
            //This will manifest as a point both preceded and followed by the same vertex
            //We want to remove the vertex, and one of the two neighbours and keep doing this until no more are left

            for (int i = 0; i < points.Count; i++)
            {
                //If we have too few points, clear the collection and give up
                if (points.Count <= 3)
                {
                    points.Clear();
                    return;
                }

                //Get the two points we're interested in (next and previous)
                var iPrev = (i + points.Count - 1) % points.Count;
                var prev = points[iPrev];

                var iVert = (i + points.Count) % points.Count;
                var vert = points[iVert];

                var iNext = (i + 1) % points.Count;
                var next = points[iNext];

                if (prev.Equals(next))
                {
                    points.RemoveAt(i);
                    points.RemoveAt(iPrev);
                    i -= 2;
                }
                else if (vert.Equals(next))
                {
                    points.RemoveAt(iVert);
                    i -= 1;
                }
            }
        }

        private static Edge WalkNextEdge(Edge edge, ref bool direction)
        {
            var v = direction ? edge.B : edge.A;
            var d = edge.Direction * (direction ? 1 : -1);

            if (v.EdgeCount == 0)
                return null;
                //throw new ArgumentException("Vertex has no connected edges!");

            if (v.EdgeCount == 1)
            {
                //Dead end! Reverse down other side of same edge
                direction = !direction;
                return edge;
            }

            if (v.EdgeCount == 2)
            {
                //Straight path! Continue down next edge in direction
                var next = v.Edges.Single(e => e != edge);
                direction = next.A.Equals(v);
                return next;

            }

            //Split choice
            var minAngle = float.MaxValue;
            var chosenDirection = true;
            Edge chosenEdge = null;

            foreach (var item in v.Edges)
            {
                if (item.Equals(edge))
                    continue;

                var bDir = item.A.Equals(v);
                var vDir = item.Direction * (bDir ? 1 : -1);
                var angle = SignedAngle(d, vDir);

                if (angle < minAngle)
                {
                    minAngle = angle;
                    chosenDirection = bDir;
                    chosenEdge = item;
                }
            }

            direction = chosenDirection;
            return chosenEdge;
        }

        private static float SignedAngle(Vector2 a, Vector2 b)
        {
            //http://stackoverflow.com/a/21486462/108234
            var dot = Vector2.Dot(a, b);
            var cross = a.X * b.Y - a.Y * b.X;
            return (float)Math.Atan2(cross, dot);
        }
    }
}
