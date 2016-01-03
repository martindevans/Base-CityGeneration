using System;
using System.Collections.Generic;
using System.Linq;
using Base_CityGeneration.Elements.Building.Design.Spec;
using Base_CityGeneration.Utilities.Numbers;
using EpimetheusPlugins.Testing.MockScripts;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Myre.Collections;

namespace Base_CityGeneration.Test.Elements.Building.Design.Spec
{
    [TestClass]
    public class FloorRangeSpecTest
    {
        [TestMethod]
        public void AssertThat_RangeWithSingleInclude_RepeatsSingleItemInIncludeCorrectNumberOfTimes()
        {
            var range = new FloorRangeSpec(new[] {
                new FloorRangeIncludeSpec("id", new NormallyDistributedValue(2, 3, 4, 1), false, true, new[] { new KeyValuePair<float, KeyValuePair<string, string>[]>(1, new [] { new KeyValuePair<string, string>("tag", "tag")
                }) }, new ConstantValue(1))
            }, new NormallyDistributedValue(1, 2, 3, 1).Transform(vary: false));

            var selected = range.Select(() => 0.5, new NamedBoxCollection(), (a, b) => ScriptReferenceFactory.Create(typeof(TestScript), Guid.NewGuid(), string.Join(",", a)));

            //Flatten runs into floors
            var floors = selected.SelectMany(a => a.Selection);

            Assert.IsTrue(2 <= floors.Count() && floors.Count() <= 3);
        }

        [TestMethod]
        public void AssertThat_RangeWithSingleInclude_OutputsNothing_WhenIncludeIsNull()
        {
            var range = new FloorRangeSpec(new[] {
                new FloorRangeIncludeSpec("id", new NormallyDistributedValue(2, 3, 4, 1), false, true, new[] { new KeyValuePair<float, KeyValuePair<string, string>[]>(1, null) }, new ConstantValue(1))
            }, new NormallyDistributedValue(1, 2, 3, 1));

            var selected = range.Select(() => 0.5, new NamedBoxCollection(), (a, b) => ScriptReferenceFactory.Create(typeof(TestScript), Guid.NewGuid(), string.Join(",", a)));

            //Flatten runs into floors
            var floors = selected.SelectMany(a => a.Selection);

            Assert.AreEqual(0, floors.Count());
        }

        [TestMethod]
        public void AssertThat_RangeWithContinuousInclude_IsNotInterrupted()
        {
            var range = new FloorRangeSpec(new[] {
                new FloorRangeIncludeSpec("id", new NormallyDistributedValue(20, 20, 20, 10), false, true, new[] {
                    new KeyValuePair<float, KeyValuePair<string, string>[]>(1, new [] { new KeyValuePair<string, string>("key", "continuous") })
                }, new ConstantValue(1)),
                new FloorRangeIncludeSpec("id", new NormallyDistributedValue(20, 30, 40, 10), false, false, new[] {
                    new KeyValuePair<float, KeyValuePair<string, string>[]>(1, new [] { new KeyValuePair<string, string>("key", "interrupt") })
                }, new ConstantValue(1))
            }, new NormallyDistributedValue(1, 2, 3, 1).Transform(vary: false));

            var r = new Random();
            var d = new NamedBoxCollection();
            var selected = range.Select(r.NextDouble, d, (a, b) => ScriptReferenceFactory.Create(typeof(TestScript), Guid.NewGuid(), string.Join(",", a))).ToArray();

            //Flatten runs into floors
            var floors = selected.SelectMany(a => a.Selection);

            //Find the first continuous floor, then check that the next 20 floors are all "continuous"
            Assert.IsTrue(floors.SkipWhile(a => a.Script.Name != "continuous").Take(20).All(a => a.Script.Name == "continuous"));
        }
    }
}
