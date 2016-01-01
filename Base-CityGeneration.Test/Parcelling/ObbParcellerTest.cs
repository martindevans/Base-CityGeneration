using Base_CityGeneration.Parcels.Parcelling;
using Base_CityGeneration.Parcels.Parcelling.Rules;
using Base_CityGeneration.Utilities.Numbers;
using EpimetheusPlugins.Procedural.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Numerics;
using System;
using System.Linq;
using Base_CityGeneration.Datastructures;
using Myre.Collections;
using Placeholder.ConstructiveSolidGeometry;

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
        public void ParcellingRespectsAreaRule()
        {
            _parceller.AddTerminationRule(new AreaRule(15, 50, 0f));
            var parcels = _parceller.GenerateParcels(new Parcel(new Vector2[] { new Vector2(0, 0), new Vector2(10, 0), new Vector2(10, 10), new Vector2(0, 10) }, new string[] { "edge" }), _random.NextDouble, new NamedBoxCollection()).ToArray();

            Assert.IsTrue(parcels.All(a => a.Area() <= 50));
        }

        [TestMethod]
        public void ParcellingDoesNotProduceOverlaps()
        {
            _parceller.AddTerminationRule(new AreaRule(15, 50, 0f));
            _parceller.AddTerminationRule(new AccessRule("edge", 0.5f));
            var parcels = _parceller.GenerateParcels(new Parcel(new Vector2[] { new Vector2(0, 0), new Vector2(10, 0), new Vector2(10, 10), new Vector2(0, 10) }, new string[] { "edge" }), _random.NextDouble, new NamedBoxCollection()).ToArray();

            //Assert.IsTrue(parcels.All(a => a.Area() <= 50));

            foreach (var parcel in parcels)
            {
                foreach (var parcel1 in parcels)
                {
                    if (parcel1.Equals(parcel))
                        continue;

                    //Test intersection (shrink them a tiny bit, to ensure they don't touch at a corner)
                    if (!SeparatingAxisTester.Intersects(parcel1.Points().Shrink(0.01f).ToArray(), parcel.Points().Shrink(0.01f).ToArray()))
                        continue;

                    Assert.Fail("Intersecting parcels");
                }
            }
        }

        private static void AssertParcel(Vector2[] expected, Vector2[] actual)
        {
            Assert.AreEqual(expected.Length, actual.Length);

            Walls.MatchUp(expected, actual);
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected[i].X, actual[i].X, 0.001f);
                Assert.AreEqual(expected[i].Y, actual[i].Y, 0.001f);
            }
        }

        [TestMethod]
        public void ParcellerCW()
        {
            _parceller.AddTerminationRule(new AreaRule(15, 50, 0.5f));
            _parceller.AddTerminationRule(new AccessRule("edge", 0.5f));
            var parcels = _parceller.GenerateParcels(new Parcel(new Vector2[] { new Vector2(0, 0), new Vector2(10, 0), new Vector2(10, 10), new Vector2(0, 10) }.Reverse(), new string[] { "edge" }), _random.NextDouble, new NamedBoxCollection()).ToArray();

            Assert.IsTrue(parcels.All(a => a.Area() <= 50));

            foreach (var parcel in parcels)
                Assert.IsTrue(parcel.Points().ConvexHullArea() > 0);
        }
    }
}
