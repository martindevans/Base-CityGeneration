using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Numerics;
using SwizzleMyVectors;

namespace Base_CityGeneration.Test
{
    [TestClass]
    public class Playground
    {
        [TestMethod]
        public void TestMethod1()
        {
            Vector2 a = Vector2.Normalize(new Vector2(0, 1));
            Vector2 b = Vector2.Normalize(new Vector2(-1, 1));

            var dot = Vector2.Dot(a, b);
            var det = a.Cross(b);
            var angle = -(float)Math.Atan2(det, dot);

            Console.WriteLine(angle);
        }
    }
}
