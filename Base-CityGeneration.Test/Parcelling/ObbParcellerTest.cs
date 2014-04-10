using System;
using System.Linq;
using Base_CityGeneration.Datastructures.Extensions;
using Base_CityGeneration.Parcelling;
using Base_CityGeneration.Parcelling.Rules;
using EpimetheusPlugins.Procedural.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Myre.Extensions;

namespace Base_CityGeneration.Test.Parcelling
{
    [TestClass]
    public class ObbParcellerTest
    {
        private ObbParceller _parceller;
        
        [TestInitialize]
        public void TestInitialize()
        {
            Random r = new Random(13);
            _parceller = new ObbParceller(r.NextDouble, 0.5f, 1.25f);
        }

        [TestMethod]
        public void ParcellingDoesNotProduceOverlaps()
        {
            _parceller.AddTerminationRule(new AreaRule(15, 50, 0.5f));
            _parceller.AddTerminationRule(new AccessRule("edge", 0.5f));
            var parcels = _parceller.GenerateParcels(new Parcel(new Vector2[] {new Vector2(0, 0), new Vector2(10, 0), new Vector2(10, 10), new Vector2(0, 10)}, new string[] {"edge"})).ToArray();

            Assert.IsTrue(parcels.All(a => a.Area() <= 50));

            AssertParcel(new[] { new Vector2(5, 0), new Vector2(5, 5), new Vector2(0, 5), new Vector2(0, 0) }, parcels[0].Points());
        }

        private static void AssertParcel(Vector2[] expected, Vector2[] actual)
        {
            Assert.AreEqual(expected.Length, actual.Length);

            Walls.MatchUp(expected, actual);
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected[i], actual[i]);
            }
        }

        [TestMethod]
        public void ParcelToMesh()
        {
            _parceller.AddTerminationRule(new AreaRule(50, 150, 0.5f));
            var parcels = _parceller.GenerateParcels(new Parcel(new Vector2[] {new Vector2(0, 0), new Vector2(90, 0), new Vector2(107, 93), new Vector2(0, 132)}, new string[] {"edge"})).ToArray();

            var mesh = parcels.ToMeshFromBinaryTree<int, int>();

            Assert.AreEqual(parcels.Length, mesh.Faces.Count());
            Assert.IsTrue(parcels.All(p => p.Points().ToArray().IsConvex(0.001f)));
            Assert.IsTrue(mesh.Faces.All(p => p.Vertices.Select(v => v.Position).ToArray().IsConvex(0.001f)));
        }
    }
}
