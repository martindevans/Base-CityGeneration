using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Xml.Linq;
using System.Numerics;

namespace Base_CityGeneration.Elements.Roads.Hyperstreamline.Tracing
{
    public class Network
    {
        private readonly Vertex[] _vertices;
        public IEnumerable<Vertex> Vertices
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<Vertex>>() != null);
                return _vertices;
            }
        }

        public Network(IEnumerable<Vertex> vertices)
        {
            Contract.Requires(vertices != null);

            _vertices = vertices.ToArray();
        }

        [ContractInvariantMethod]
        private void ObjectInvariants()
        {
            Contract.Invariant(_vertices != null);
        }

        public string ToSvg(IEnumerable<Region> regions = null)
        {
            var g = new XElement("g",
                new XAttribute("transform", "translate(10, 10)")
            );

            var min = new Vector2(float.MaxValue);
            var max = new Vector2(float.MinValue);
            foreach (var vertex in _vertices)
            {
                foreach (var edge in vertex.Edges)
                {
                    if (Equals(edge.A, vertex))
                    {
                        g.Add(new XElement("line",
                            new XAttribute("x1", edge.A.Position.X),
                            new XAttribute("y1", edge.A.Position.Y),
                            new XAttribute("x2", edge.B.Position.X),
                            new XAttribute("y2", edge.B.Position.Y),
                            new XAttribute("style", string.Format("stroke:rgb(0,0,0);stroke-width:{0};stroke-linecap:round", Math.Max(1, edge.Streamline.Width)))
                        ));

                        min = new Vector2(Math.Min(min.X, edge.A.Position.X), Math.Min(min.Y, edge.A.Position.Y));
                        max = new Vector2(Math.Max(max.X, edge.A.Position.X), Math.Max(max.Y, edge.A.Position.Y));
                    }
                }
            }

            if (regions != null)
            {
                int i = 0;
                foreach (var region in regions)
                {
                    var points = region.Vertices
                        .Select(a => string.Format("{0},{1}", a.X, a.Y));
                    var path = string.Join(" ", points);

                    float hue = ((0.618033988749895f * i) % 1) * 360;

                    g.Add(new XElement("polygon", 
                        new XAttribute("points", path),
                        new XAttribute("style", string.Format("fill:hsla({0},100%,50%,0.1);", hue))
                    ));

                    i++;
                }
            }

            var svg = new XElement("svg", new XAttribute("width", max.X + 20), new XAttribute("height", max.Y + 20));
            svg.AddFirst(g);

            var doc = new XDocument();
            doc.Add(svg);

            return doc.ToString();
        }
    }
}
