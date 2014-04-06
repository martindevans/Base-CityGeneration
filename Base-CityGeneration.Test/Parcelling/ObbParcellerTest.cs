using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Base_CityGeneration.Datastructures;
using Base_CityGeneration.Datastructures.Extensions;
using Base_CityGeneration.Parcelling;
using Base_CityGeneration.Parcelling.Rules;
using EpimetheusPlugins.Procedural.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;

namespace Base_CityGeneration.Test.Parcelling
{
    [TestClass]
    public class ObbParcellerTest
    {
        private ObbParceller<MyParcelElement> _parceller;
        
        [TestInitialize]
        public void TestInitialize()
        {
            Random r = new Random(1);
            _parceller = new ObbParceller<MyParcelElement>(r.NextDouble, 0, 0);
        }

        [TestMethod]
        public void ParcellingDoesNotProduceOverlaps()
        {
            _parceller.AddTerminationRule(new AreaRule<MyParcelElement>(15, 50, 0.5f));
            var parcels = _parceller.GenerateParcels(new Parcel<MyParcelElement>(new Vector2[] {new Vector2(0, 0), new Vector2(10, 0), new Vector2(10, 10), new Vector2(0, 10)}, new string[] {"edge"})).ToArray();

            Assert.IsTrue(parcels.All(a => a.Area() <= 50));

            AssertParcel(new[] { new Vector2(5, 0), new Vector2(5, 5), new Vector2(0, 5), new Vector2(0, 0) }, parcels[0].Points());
        }

        private void AssertParcel(Vector2[] expected, Vector2[] actual)
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
            _parceller.AddTerminationRule(new AreaRule<MyParcelElement>(15, 50, 0.5f));
            var parcels = _parceller.GenerateParcels(new Parcel<MyParcelElement>(new Vector2[] {new Vector2(0, 0), new Vector2(10, 0), new Vector2(10, 10), new Vector2(0, 10)}, new string[] {"edge"})).ToArray();

            var mesh = parcels.ToMeshFromBinaryTree<MyParcelElement, int, int, int>();

            Assert.AreEqual(parcels.Length, mesh.Faces.Count());
        }

        private class MyParcelElement : IParcelElement<MyParcelElement>
        {
            public Parcel<MyParcelElement> Parcel { get; set; }
        }
    }
}
