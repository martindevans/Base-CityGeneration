using System.Numerics;
using Base_CityGeneration.Elements.Building.Design.Spec.Markers.Algorithms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Myre;
using Myre.Collections;

namespace Base_CityGeneration.Test.Elements.Building.Design.Spec.Markers.Algorithms
{
    [TestClass]
    public class MetaSetTest
        : BaseAlgorithmTest
    {
        [TestMethod]
        public void AssertThat_MetaSet_UnwrapsAndSetsValue()
        {
            var input = new Vector2[] {
                new Vector2(10, 10),
                new Vector2(10, -10),
                new Vector2(-10, -10),
                new Vector2(-10, 10)
            };

            var meta = new NamedBoxCollection();

            var container = new MetaSet.Container {
                Key = "test value",
                Type = "System.Boolean",
                Value = "true"
            };
            var set = container.Unwrap();

            Test(set, input, meta);

            Assert.IsTrue(meta.GetValue(new TypedName<bool>("test value"), false));
        }

        [TestMethod]
        public void AssertThat_MetaSet_SetsValue()
        {
            var input = new Vector2[] {
                new Vector2(10, 10),
                new Vector2(10, -10),
                new Vector2(-10, -10),
                new Vector2(-10, 10)
            };

            var meta = new NamedBoxCollection();
            var set = new MetaSet<bool>("test value", true);

            Test(set, input, meta);

            Assert.IsTrue(meta.GetValue(new TypedName<bool>("test value"), false));
        }
    }
}
