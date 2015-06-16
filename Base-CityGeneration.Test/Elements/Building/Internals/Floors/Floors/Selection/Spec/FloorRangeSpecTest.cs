using System;
using System.Collections.Generic;
using System.Linq;
using Base_CityGeneration.Elements.Building.Internals.Floors.Selection.Spec;
using EpimetheusPlugins.Scripts;
using EpimetheusPlugins.Testing.MockScripts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Base_CityGeneration.Test.Elements.Building.Internals.Floors.Floors.Selection.Spec
{
    [TestClass]
    public class FloorRangeSpecTest
    {
        [TestMethod]
        public void AssertThat_RangeWithSingleInclude_RepeatsSingleItemInIncludeCorrectNumberOfTimes()
        {
            var range = new FloorRangeSpec(new[] {
                new FloorRangeIncludeSpec(2, 3, 4, 1, false, true, new[] { new KeyValuePair<float, string[]>(1, new [] { "tag" }) }, null)
            }, new HeightSpec(1, 2, 3, 1, null, false));

            var selected = range.Select(() => 0.5, new ScriptReference[0], a => ScriptReferenceFactory.Create(typeof(TestScript), Guid.NewGuid(), string.Join(",", a)), null);

            Assert.IsTrue(2 <= selected.Count() && selected.Count() <= 3);
        }

        [TestMethod]
        public void AssertThat_RangeWithSingleInclude_OutputsNothing_WhenIncludeIsNull()
        {
            var range = new FloorRangeSpec(new[] {
                new FloorRangeIncludeSpec(2, 3, 4, 1, false, true, new[] { new KeyValuePair<float, string[]>(1, null) }, null)
            }, new HeightSpec(1, 2, 3, 1, null, false));

            var selected = range.Select(() => 0.5, new ScriptReference[0], a => ScriptReferenceFactory.Create(typeof(TestScript), Guid.NewGuid(), string.Join(",", a)), null);

            Assert.AreEqual(0, selected.Count());
        }

        [TestMethod]
        public void AssertThat_RangeWithContinuousInclude_IsNotInterrupted()
        {
            var range = new FloorRangeSpec(new[] {
                new FloorRangeIncludeSpec(20, 20, 20, 10, false, true, new[] { new KeyValuePair<float, string[]>(1, new [] { "continuous" }) }, null),
                new FloorRangeIncludeSpec(20, 30, 40, 10, false, false, new[] { new KeyValuePair<float, string[]>(1, new [] { "interrupt" }) }, null)
            }, new HeightSpec(1, 2, 3, 1, null, false));

            Random r = new Random();
            var selected = range.Select(r.NextDouble, new ScriptReference[0], a => ScriptReferenceFactory.Create(typeof(TestScript), Guid.NewGuid(), string.Join(",", a)), null).ToArray();

            //Find the first "continuous" floor, then check that every single one of the next 20 is also "continuous"
            var startCont = selected.Select((a, i) => new {a, i}).Where(a => a.a.Script.Name == "continuous").Select(a => a.i).First();
            for (int i = 0; i < 20; i++)
                Assert.AreEqual("continuous", selected[i + startCont].Script.Name);
        }
    }
}
