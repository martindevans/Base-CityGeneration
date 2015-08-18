using Base_CityGeneration.Utilities.Numbers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Myre;
using Myre.Collections;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace Base_CityGeneration.Test.Utilities.Numbers
{
    [TestClass]
    public class MetaValueTest
    {
        [TestMethod]
        public void AssertThat_MetaValue_GetsFloatFromMetadata()
        {
            MetaValue m = new MetaValue("hello", new ConstantValue(1));

            var v = m.SelectFloatValue(null, new NamedBoxCollection {
                { new TypedName<float>("hello"), 2f },
                { new TypedName<int>("hello"), 1 }
            });

            Assert.AreEqual(2f, v);
        }

        [TestMethod]
        public void AssertThat_MetaValue_GetIntFromMetadata()
        {
            MetaValue m = new MetaValue("hello", new ConstantValue(1));

            var v = m.SelectIntValue(null, new NamedBoxCollection {
                { new TypedName<float>("hello"), 2f },
                { new TypedName<int>("hello"), 1 }
            });

            Assert.AreEqual(1, v);
        }
    }
}
