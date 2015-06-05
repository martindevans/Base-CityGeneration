using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace Base_CityGeneration.Test
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            Assert.AreEqual(0, (int)Class1.DefaultValue(typeof(int)));
        }

        [TestMethod]
        public void MethodName()
        {
            var d = (Task<int>)Class1.GetDefaultTask(typeof(Task<int>));
            Assert.AreEqual(0, d.Result);
        }
    }
}
