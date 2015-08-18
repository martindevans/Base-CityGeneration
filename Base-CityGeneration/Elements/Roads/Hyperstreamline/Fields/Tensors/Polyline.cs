using System;
using EpimetheusPlugins.Procedural.Utilities;
using Microsoft.Xna.Framework;
using Myre.Collections;

namespace Base_CityGeneration.Elements.Roads.Hyperstreamline.Fields.Tensors
{
    public class Polyline
        : ITensorField
    {
        private readonly Vector2[] _points;
        private readonly float _decay;

        public Polyline(Vector2[] points, float decay)
        {
            _points = points;
            _decay = decay;
        }

        public void Sample(ref Vector2 position, out Tensor result)
        {
            result = new Tensor(0, 0);

            for (int i = 0; i < _points.Length - 1; i++)
            {
                var start = _points[i];
                var end = _points[i + 1];
                var dir = Vector2.Normalize(end - start);

                var angle = Math.Atan2(dir.Y, dir.X) + MathHelper.PiOver2;
                var tensor = Tensor.Normalize(Tensor.FromRTheta(1, angle));

                var dist = Geometry2D.DistanceFromPointToLineSegment(position, new LineSegment2D(start, end));
                var decay = PointDistanceDecayField.DistanceDecay(dist * dist, _decay);

                result += decay * tensor;
            }
        }

        internal class Container
            : ITensorFieldContainer
        {
            public float Decay { get; set; }

            public Vector2[] Points { get; set; }

            public ITensorField Unwrap(Func<double> random, INamedDataCollection metadata)
            {
                return new Polyline(Points, Decay);
            }
        }
    }
}
