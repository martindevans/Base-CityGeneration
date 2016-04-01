using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Base_CityGeneration.Datastructures.HalfEdge;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Base_CityGeneration.Test.Datastructures.HalfEdge
{
    [TestClass]
    public class MeshTest
    {
        [TestMethod]
        public void Playground()
        {
            var m = new Mesh<int, int, int>();

            var tl = m.GetOrConstructVertex(new Vector2(0, 0));
            var tm = m.GetOrConstructVertex(new Vector2(5, 0));
            var tr = m.GetOrConstructVertex(new Vector2(10, 0));

            var ml = m.GetOrConstructVertex(new Vector2(0, -5));
            var mm = m.GetOrConstructVertex(new Vector2(5, -5));

            var bl = m.GetOrConstructVertex(new Vector2(0, -10));
            var br = m.GetOrConstructVertex(new Vector2(10, -10));

            var aFace = m.GetOrConstructFace(tl, tm, mm, ml);

            var bFace = m.GetOrConstructFace(bl, ml, tm, br);
        }
    }
}
