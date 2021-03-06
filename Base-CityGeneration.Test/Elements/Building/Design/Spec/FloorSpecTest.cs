﻿using System;
using System.Collections.Generic;
using System.Linq;
using Base_CityGeneration.Elements.Building.Design.Spec;
using Base_CityGeneration.Utilities.Extensions;
using Base_CityGeneration.Utilities.Numbers;
using EpimetheusPlugins.Testing.MockScripts;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Myre.Collections;

namespace Base_CityGeneration.Test.Elements.Building.Design.Spec
{
    [TestClass]
    public class FloorSpecTest
    {
        [TestMethod]
        public void AssertThat_FloorSpec_SelectsSingleItem_FromSingleChoice()
        {
            FloorSpec spec = new FloorSpec(new[] {
                new KeyValuePair<float, KeyValuePair<string, string>[]>(1, new [] { new KeyValuePair<string, string>("key", "tag") })
            }, new NormallyDistributedValue(1, 2, 3, 1).Transform(vary: false));

            var selected = spec.Select(() => 0.5, new NamedBoxCollection(), (a, b) => ScriptReferenceFactory.Create(typeof(TestScript), Guid.NewGuid(), string.Join(",", a.Select(t => t.Value))));

            Assert.AreEqual(1, selected.Count());
            Assert.AreEqual("tag", selected.Single().Selection.Single().Script.Name);
        }

        [TestMethod]
        public void AssertThat_FloorSpec_SelectsNothing_FromNull()
        {
            FloorSpec spec = new FloorSpec(new[] {
                new KeyValuePair<float, KeyValuePair<string, string>[]>(1, null)
            }, new NormallyDistributedValue(1, 2, 3, 1).Transform(vary: false));

            var selected = spec.Select(() => 0.5, new NamedBoxCollection(), (a, b) => ScriptReferenceFactory.Create(typeof(TestScript), Guid.NewGuid(), string.Join(",", a)));

            var floors = selected.SelectMany(a => a.Selection);

            Assert.AreEqual(0, floors.Count());
        }
    }
}
