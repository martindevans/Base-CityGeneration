using Base_CityGeneration.Elements.Building.Internals.Floors.Selection.Spec;
using Base_CityGeneration.Utilities;
using Base_CityGeneration.Utilities.Numbers;
using EpimetheusPlugins.Scripts;
using EpimetheusPlugins.Testing.MockScripts;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Base_CityGeneration.Test.Elements.Building.Internals.Floors.Floors.Selection.Spec
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

            var selected = spec.Select(() => 0.5, a => ScriptReferenceFactory.Create(typeof(TestScript), Guid.NewGuid(), string.Join(",", a)));

            Assert.AreEqual(1, selected.Count());
            Assert.AreEqual("tag", selected.Single().Script.Name);
        }

        [TestMethod]
        public void AssertThat_FloorSpec_SelectsNothing_FromNull()
        {
            FloorSpec spec = new FloorSpec(new[] {
                new KeyValuePair<float, string[]>(1, null)
            }, new NormallyDistributedValue(1, 2, 3, 1, false));

            var selected = spec.Select(() => 0.5, a => ScriptReferenceFactory.Create(typeof(TestScript), Guid.NewGuid(), string.Join(",", a)));

            Assert.AreEqual(0, selected.Count());
        }
    }
}
