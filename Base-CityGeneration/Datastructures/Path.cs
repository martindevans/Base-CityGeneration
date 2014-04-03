﻿using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Myre.Extensions;

namespace Base_CityGeneration.Datastructures
{
    public class Path
    {
        private readonly Vector2[][] _quadrangles;
        public IEnumerable<Vector2[]> Quadrangles
        {
            get { return _quadrangles; }
        }

        public Path(params Segment[] segments)
        {
            _quadrangles = CalculateQuadrangles(segments, CalculateNormals(segments));
        }

        private static Vector2[] CalculateNormals(IList<Segment> segments)
        {
            Vector2[] normals = new Vector2[segments.Count];

            for (int i = 0; i < segments.Count; i++)
            {
                var segment = segments[i];

                Vector2 startCrossDirection;
                if (i == 0)
                {
                    var next = segments[1];
                    startCrossDirection = Vector2.Normalize(next.Position - segment.Position).Perpendicular();
                }
                else if (i == segments.Count - 1)
                {
                    var previous = segments[i - 1];
                    startCrossDirection = Vector2.Normalize(segment.Position - previous.Position).Perpendicular();
                }
                else
                {
                    var prev = segments[i - 1];
                    var next = segments[i + 1];
                    var dirInNorm = Vector2.Normalize(segment.Position - prev.Position).Perpendicular();
                    var dirOutNorm = Vector2.Normalize(next.Position - segment.Position).Perpendicular();
                    startCrossDirection = Vector2.Lerp(dirInNorm, dirOutNorm, 0.5f);
                }

                normals[i] = startCrossDirection;
            }

            return normals;
        }

        private static Vector2[][] CalculateQuadrangles(IList<Segment> segments, IList<Vector2> normals)
        {
            var quads = new Vector2[segments.Count - 1][];

            for (int i = 0; i < normals.Count - 1; i++)
            {
                var segment = segments[i];
                var segmentNext = segments[i + 1];
                var normal = normals[i];
                var normalNext = normals[i + 1];

                quads[i] = new Vector2[]
                {
                    segment.Position + normal * segment.Width * 0.5f,
                    segmentNext.Position + normalNext * segmentNext.Width * 0.5f,
                    segmentNext.Position - normalNext * segmentNext.Width * 0.5f,
                    segment.Position - normal * segment.Width * 0.5f,
                };
            }

            return quads;
        }

        public bool Intersects(Rectangle r)
        {
            if (_quadrangles.Any(q => SeparatingAxisTester.Intersects(r, q)))
                return true;

            return false;
        }

        public struct Segment
        {
            public readonly Vector2 Position;
            public readonly float Width;

            public Segment(Vector2 position, float width)
            {
                Position = position;
                Width = width;
            }
        }
    }
}
