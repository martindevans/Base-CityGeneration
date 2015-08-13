using System;
using System.Linq;
using Base_CityGeneration.Datastructures.Extensions;
using Base_CityGeneration.Parcels.Parcelling;
using Base_CityGeneration.Parcels.Parcelling.Rules;
using Base_CityGeneration.Utilities.Numbers;
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
        private readonly Random _random = new Random(10);
        
        [TestInitialize]
        public void TestInitialize()
        {
            _parceller = new ObbParceller() {
                NonOptimalOabbChance = 0.5f,
                NonOptimalOabbMaxRatio = 1.25f,
                SplitPointGenerator = new ConstantValue(0)
            };
        }

        [TestMethod]
        public void ParcellingDoesNotProduceOverlaps()
        {
            _parceller.AddTerminationRule(new AreaRule(15, 50, 0.5f));
            _parceller.AddTerminationRule(new AccessRule("edge", 0.5f));
            var parcels = _parceller.GenerateParcels(new Parcel(new Vector2[] {new Vector2(0, 0), new Vector2(10, 0), new Vector2(10, 10), new Vector2(0, 10)}, new string[] {"edge"}), _random.NextDouble).ToArray();

            Assert.IsTrue(parcels.All(a => a.Area() <= 50));

            AssertParcel(new[] { new Vector2(5, 0), new Vector2(5, 5), new Vector2(0, 5), new Vector2(0, 0) }, parcels[1].Points());
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
        public void ParcellerCCW()
        {
            _parceller.AddTerminationRule(new AreaRule(15, 50, 0.5f));
            _parceller.AddTerminationRule(new AccessRule("edge", 0.5f));
            var parcels = _parceller.GenerateParcels(new Parcel(new Vector2[] { new Vector2(0, 0), new Vector2(10, 0), new Vector2(10, 10), new Vector2(0, 10) }.Reverse(), new string[] { "edge" }), _random.NextDouble).ToArray();

            Assert.IsTrue(parcels.All(a => a.Area() <= 50));

            AssertParcel(new[] { new Vector2(5, 0), new Vector2(5, 5), new Vector2(0, 5), new Vector2(0, 0) }, parcels[1].Points());
        }
    }
}
