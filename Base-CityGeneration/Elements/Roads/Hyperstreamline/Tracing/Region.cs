using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Base_CityGeneration.Elements.Roads.Hyperstreamline.Tracing
{
    public class Region
    {
        private readonly Vector2[] _vertices;
        public IEnumerable<Vector2> Vertices { get { return _vertices; } }

        private readonly Vector2 _min;
        public Vector2 Min
        {
            get
            {
                return _min;
            }
        }

        private readonly Vector2 _max;
        public Vector2 Max
        {
            get
            {
                return _max;
            }
        }

        public Region(List<Vector2> vertices)
        {
            _vertices = vertices.ToArray();

            _min = new Vector2(float.MaxValue);
            _max = new Vector2(float.MinValue);
            foreach (var vertex in vertices)
            {
                _min = new Vector2(Math.Min(vertex.X, _min.X), Math.Min(vertex.Y, _min.Y));
                _max = new Vector2(Math.Max(vertex.X, _max.X), Math.Max(vertex.Y, _max.Y));
            }
        }

        public bool IsClockwise()
        {
            //http://stackoverflow.com/a/1165943/108234

            float sum = 0;

            for (int i = 0; i < _vertices.Length; i++)
            {
                var a = _vertices[i];
                var b = _vertices[(i + 1) % _vertices.Length];

                var n = (b.X - a.X) * (b.Y - a.Y);

                sum += n;
            }

            return sum > 0;
        }

        /// <summary>
        /// Determine whether given 2D point lies within 
        /// the polygon.
        /// 
        /// Written by Jeremy Tammik, Autodesk, 2009-09-23, 
        /// based on code that I wrote back in 1996 in C++, 
        /// which in turn was based on C code from the 
        /// article "An Incremental Angle Point in Polygon 
        /// Test" by Kevin Weiler, Autodesk, in "Graphics 
        /// Gems IV", Academic Press, 1994.
        /// 
        /// Copyright (C) 2009 by Jeremy Tammik. All 
        /// rights reserved.
        /// 
        /// This code may be freely used. Please preserve 
        /// this comment.
        /// </summary>
        public bool PointInPolygon(Vector2 point)
        {
            unsafe
            {
                fixed (Vector2* arr = &_vertices[0])
                {
                    Vector2* pointPtr = &point;

                    //http://thebuildingcoder.typepad.com/blog/2010/12/point-in-polygon-containment-algorithm.html

                    // initialize
                    int quad = GetQuadrant(*arr, *pointPtr);
                    int angle = 0;

                    // loop on all vertices of polygon
                    int n = _vertices.Length;
                    for (int i = 0; i < n; ++i)
                    {
                        Vector2* vertex = arr + i;
                        Vector2* nextVertex = arr + ((i + 1 < n) ? i + 1 : 0);

                        // calculate quadrant and delta from last quadrant
                        int nextQuad = GetQuadrant(*nextVertex, *pointPtr);
                        int delta = (nextQuad - quad);

                        AdjustDelta(ref delta, vertex, nextVertex, pointPtr);

                        // add delta to total angle sum
                        angle = (angle + delta);

                        // increment for next step
                        quad = nextQuad;
                    }

                    // complete 360 degrees (angle of + 4 or -4 ) 
                    // means inside

                    return (angle == +4) || (angle == -4);

                    // odd number of windings rule:
                    // if (angle & 4) return INSIDE; else return OUTSIDE;
                    // non-zero winding rule:
                    // if (angle != 0) return INSIDE; else return OUTSIDE;
                }
            }
        }

        static unsafe void AdjustDelta(ref int delta, Vector2* vertex, Vector2* nextVertex, Vector2* p)
        {
            switch (delta)
            {
                // make quadrant deltas wrap around:
                case 3: delta = -1; break;
                case -3: delta = 1; break;
                // check if went around point cw or ccw:
                case 2:
                case -2:
                    if (XIntercept(vertex, nextVertex, p->Y) > p->X)
                        delta = -delta;
                    break;
            }
        }

        static unsafe float XIntercept(Vector2* p, Vector2* q, float y)
        {
            if (Math.Abs(p->Y - q->Y) < float.Epsilon)
              throw new ArgumentException("unexpected horizontal segment");

            return q->X - ((q->Y - y) * ((p->X - q->X) / (p->Y - q->Y)));
        }

        static int GetQuadrant(Vector2 vertex, Vector2 p)
        {
            return ((vertex.X > p.X)
                 ? ((vertex.Y > p.Y) ? 0 : 3)
                 : ((vertex.Y > p.Y) ? 1 : 2));
        }

        public Region Flip()
        {
            Array.Reverse(_vertices);
            return this;
        }
    }
}
