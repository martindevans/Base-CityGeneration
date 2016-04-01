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

        #region room walls
        [TestMethod]
        public void AssertThat_CreateWalls_CreateWallsAndSections()
        {
            var mesh = new Mesh<int, int, int>();

            mesh.GetOrConstructFace(
                mesh.GetOrConstructVertex(new Vector2(0, 0)),
                mesh.GetOrConstructVertex(new Vector2(0, 10)),
                mesh.GetOrConstructVertex(new Vector2(10, 10)),
                mesh.GetOrConstructVertex(new Vector2(10, 0))
            ).Tag = 0;

            var result = mesh.CreateRoomWalls(a => 1f, a => a.IsCorner ? 1 : 2);

            Console.WriteLine(Draw(mesh, a => "grey"));

            Assert.AreEqual(9, result.Faces.Count());
            Assert.AreEqual(4, result.Faces.Select(f => f.Tag).Count(t => t == 1));
            Assert.AreEqual(4, result.Faces.Select(f => f.Tag).Count(t => t == 2));
            Assert.AreEqual(64, result.Faces.SingleOrDefault(f => f.Tag == 0).Area());
        }

        [TestMethod]
        public void AssertThat_CreateWalls_CreateWallsAndSections_WithConcaveRooms()
        {
            var mesh = new Mesh<int, int, int>();

            mesh.GetOrConstructFace(
                mesh.GetOrConstructVertex(new Vector2(0, 0)),
                mesh.GetOrConstructVertex(new Vector2(0, 10)),
                mesh.GetOrConstructVertex(new Vector2(15, 10)),
                mesh.GetOrConstructVertex(new Vector2(15, 5)),
                mesh.GetOrConstructVertex(new Vector2(10, 5)),
                mesh.GetOrConstructVertex(new Vector2(10, 0))
                ).Tag = 0;

            mesh.GetOrConstructFace(
                mesh.GetOrConstructVertex(new Vector2(15, -10)),
                mesh.GetOrConstructVertex(new Vector2(0, -10)),
                mesh.GetOrConstructVertex(new Vector2(0, 0)),
                mesh.GetOrConstructVertex(new Vector2(10, 0)),
                mesh.GetOrConstructVertex(new Vector2(10, 5)),
                mesh.GetOrConstructVertex(new Vector2(15, 5))

            ).Tag = 0;

            var result = mesh.CreateRoomWalls(a => 1f, a => a.IsCorner ? 1 : 2);

            Console.WriteLine(Draw(mesh, a => "grey"));

            Assert.AreEqual(26, result.Faces.Count());
            Assert.AreEqual(12, result.Faces.Select(f => f.Tag).Count(t => t == 1));
            Assert.AreEqual(12, result.Faces.Select(f => f.Tag).Count(t => t == 2));
        }

        [TestMethod]
        public void AssertThat_CreateWalls_CreateWallsAndSections_WithMultipleRooms()
        {
            var mesh = new Mesh<int, int, int>();

            mesh.GetOrConstructFace(
                mesh.GetOrConstructVertex(new Vector2(0, 0)),
                mesh.GetOrConstructVertex(new Vector2(0, 10)),
                mesh.GetOrConstructVertex(new Vector2(10, 10)),
                mesh.GetOrConstructVertex(new Vector2(10, 0))
            ).Tag = 0;
            mesh.GetOrConstructFace(
                mesh.GetOrConstructVertex(new Vector2(10, 0)),
                mesh.GetOrConstructVertex(new Vector2(10, 10)),
                mesh.GetOrConstructVertex(new Vector2(20, 10)),
                mesh.GetOrConstructVertex(new Vector2(20, 0))
            ).Tag = 0;
            mesh.GetOrConstructFace(
                mesh.GetOrConstructVertex(new Vector2(0, -10)),
                mesh.GetOrConstructVertex(new Vector2(0, 0)),
                mesh.GetOrConstructVertex(new Vector2(10, 0)),
                mesh.GetOrConstructVertex(new Vector2(20, 0)),
                mesh.GetOrConstructVertex(new Vector2(20, -10))
            ).Tag = 0;

            var result = mesh.CreateRoomWalls(a => 1f, a => a.IsCorner ? 1 : 2);

            Console.WriteLine(Draw(mesh, a => "grey"));

            //You may think there should be an extra few wall sections in here (around the join point of the three rooms)
            //Unfortunately shrinking removes vertices along dead straight walls, and so this doesn't happen
            //Fixing this would be extremely difficult, deal with it

            Assert.AreEqual(12, result.Faces.Select(f => f.Tag).Count(t => t == 1));
            Assert.AreEqual(12, result.Faces.Select(f => f.Tag).Count(t => t == 2));
            Assert.AreEqual(27, result.Faces.Count());
        }
        #endregion
    }
}
