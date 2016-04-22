using System;
using System.Linq;
using System.Numerics;
using Base_CityGeneration.Datastructures.Extensions;
using Base_CityGeneration.Datastructures.HalfEdge;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PrimitiveSvgBuilder;

namespace Base_CityGeneration.Test.Elements.Building.Internals.Floors.Plan
{
    [TestClass]
    public class MeshExtensionsTest
    {
        private static SvgBuilder Draw<TA, TB, TC>(Mesh<TA, TB, TC> mesh, Func<TC, string> faceColor)
        {
            var svg = new SvgBuilder(10);
            foreach (var face in mesh.Faces)
                svg.Outline(face.Vertices.Select(v => v.Position).ToArray(), stroke: "none", fill: faceColor(face.Tag));
            foreach (var edge in mesh.HalfEdges.Where(a => a.IsPrimaryEdge))
                svg.Line(edge.StartVertex.Position, edge.EndVertex.Position, 1, "black");
            foreach (var vertex in mesh.Vertices)
                svg.Circle(vertex.Position, 0.2f, "black");
            return svg;
        }
    }
}
