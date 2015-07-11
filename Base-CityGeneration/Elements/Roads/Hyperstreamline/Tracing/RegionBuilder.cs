using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace Base_CityGeneration.Elements.Roads.Hyperstreamline.Tracing
{
    class RegionBuilder
    {
        private readonly HashSet<Edge> _unprocessed;
        private readonly HashSet<KeyValuePair<bool, Edge>> _halfProcessed = new HashSet<KeyValuePair<bool, Edge>>();

        public RegionBuilder(IEnumerable<Vertex> vertices)
        {
            _unprocessed = new HashSet<Edge>(vertices.SelectMany(v => v.Edges));
        }

        public IEnumerable<Region> Regions()
        {
            //Empty out these two collections (it's larger, let's keep it as small as possible)
            while (_unprocessed.Count > 0 || _halfProcessed.Count > 0)
            {
                //Empty out the half processed collection first
                if (_halfProcessed.Count > 0)
                {
                    var e = _halfProcessed.First();
                    var boundary = WalkRegionBoundary(e.Value, e.Key, _unprocessed, _halfProcessed);
                    if (boundary != null)
                        yield return boundary;

                    continue;
                }

                //If we have no half processed edges process a totally untouched edge (probably generating a bunch of half processed edges at the same time)
                if (_unprocessed.Count > 0)
                {
                    var e = _unprocessed.First();
                    var boundary = WalkRegionBoundary(e, true, _unprocessed, _halfProcessed);
                    if (boundary != null)
                        yield return boundary;
                }
            }
        }

        private static Region WalkRegionBoundary(Edge start, bool direction, ISet<Edge> unprocessed, ISet<KeyValuePair<bool, Edge>> halfProcessed)
        {
            //We only want to trace boundary streamlines (width zero) on the inside, not the outside
            //Skip this streamline if it is a clockwise boundary streamline (and update collections)
            if (start.Streamline.Width == 0 && direction)
            {
                halfProcessed.Remove(new KeyValuePair<bool, Edge>(true, start));
                if (unprocessed.Remove(start))
                    halfProcessed.Add(new KeyValuePair<bool, Edge>(false, start));
                return null;
            }

            var points = new List<Vector2>();

            var e = start;
            do
            {
                //If this edge was previously unprocessed, the other side has not been touched yet, queue it up to be processed
                if (unprocessed.Remove(e))
                    halfProcessed.Add(new KeyValuePair<bool, Edge>(!direction, e));
                else if (!halfProcessed.Remove(new KeyValuePair<bool, Edge>(direction, e)))
                    return null;

                //Accumulate edges
                var p = (direction ? e.B : e.A).Position;
                points.Add(p);

                //Step to next edge
                e = WalkNextEdge(e, ref direction);

                if (e == null)
                    return null;

            } while (e != start);

            if (points.Count >= 3)
                return new Region(points);

            return null;
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
