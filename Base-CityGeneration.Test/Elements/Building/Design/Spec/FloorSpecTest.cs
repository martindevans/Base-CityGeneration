using System;
using System.Collections.Generic;
using System.Linq;
using Base_CityGeneration.Elements.Building.Design.Spec;
using Base_CityGeneration.Utilities.Numbers;
using EpimetheusPlugins.Testing.MockScripts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Base_CityGeneration.Test.Elements.Building.Design.Spec
{
    [TestClass]
    public class FloorSpecTest
    {
        [TestMethod]
        public void AssertThat_FloorSpec_SelectsSingleItem_FromSingleChoice()
        {
            FloorSpec spec = new FloorSpec(new[] {
                new KeyValuePair<float, string[]>(1, new [] { "tag" })
            }, new NormallyDistributedValue(1, 2, 3, 1, false));

            var selected = spec.Select(() => 0.5, null, a => ScriptReferenceFactory.Create(typeof(TestScript), Guid.NewGuid(), string.Join(",", a)));

            Assert.AreEqual(1, selected.Count());
            Assert.AreEqual("tag", selected.Single().Selection.Single().Script.Name);
        }

        [TestMethod]
        public void AssertThat_FloorSpec_SelectsNothing_FromNull()
        {
            FloorSpec spec = new FloorSpec(new[] {
                new KeyValuePair<float, string[]>(1, null)
            }, new NormallyDistributedValue(1, 2, 3, 1, false));

            var selected = spec.Select(() => 0.5, null, a => ScriptReferenceFactory.Create(typeof(TestScript), Guid.NewGuid(), string.Join(",", a)));

            var floors = selected.SelectMany(a => a.Selection);

            Assert.AreEqual(0, floors.Count());
        }
    }
}
