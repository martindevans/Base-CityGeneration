using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Base_CityGeneration.Utilities.SVG
{
    internal class SvgRenderer
    {
        private readonly float _scale;

        private Vector2 _min = new Vector2(float.MaxValue);
        private Vector2 _max = new Vector2(float.MinValue);

        private readonly List<string> _parts = new List<string>();

        public SvgRenderer(float scale)
        {
            _scale = scale;
        }

        public void AddOutline(IReadOnlyList<Vector2> shape, string color = "blue", bool closed = true)
        {
            _parts.Add(ToSvgPath(shape, _scale, color, closed));

            _min = Vector2.Min(_min, shape.Aggregate(Vector2.Min));
            _max = Vector2.Max(_max, shape.Aggregate(Vector2.Max));
        }

        private static string ToSvgPath(IReadOnlyList<Vector2> shape, float scale, string color, bool closed)
        {
            var builder = new StringBuilder("<path fill=\"none\" stroke=\"" + color + "\" d=\"");

            builder.Append(string.Format("M {0} {1} ", shape[0].X * scale, shape[0].Y * scale));
            for (var i = 1; i < shape.Count; i++)
                builder.Append(string.Format("L {0} {1} ", shape[i].X * scale, shape[i].Y * scale));

            if (closed)
                builder.Append("Z");
            builder.Append("\"></path>");

            return builder.ToString();
        }

        public string Render()
        {
            var extent = (_max - _min) * _scale;

            return string.Format("<svg width=\"{0}\" height=\"{1}\"><g transform=\"translate({2}, {3})\">{4}</g></svg>",
                extent.X, extent.Y,
                -_min.X * _scale, -_min.Y * _scale,
                string.Join("", _parts)
            );
        }
    }
}
