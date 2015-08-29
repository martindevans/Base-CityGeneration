using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Numerics;

namespace Base_CityGeneration.Test
{
    [TestClass]
    public class Playground
    {
        [TestMethod]
        public void TestMethod1()
        {
            Vector2 a = Vector2.Normalize(new Vector2(0, 1));
            Vector2 b = Vector2.Normalize(new Vector2(-1, 0));

            Console.WriteLine(Vector2.Dot(a, b));
        }
    }
}
