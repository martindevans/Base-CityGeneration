using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Base_CityGeneration.Elements.Building.Internals.Floors.Selection;
using Base_CityGeneration.Elements.Building.Internals.Floors.Selection.Spec.Ref;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Base_CityGeneration.Test.Elements.Building.Internals.Floors.Selection.Spec.Ref
{
    [TestClass]
    public class TaggedRefTest
    {
        readonly FloorSelection[] _floors = {
            new FloorSelection("top", new []{ "top" }, null, 0, 12),
            new FloorSelection("a", new []{ "a" }, null, 0, 11),
            new FloorSelection("a", new []{ "a", "x" }, null, 0, 10),
            new FloorSelection("a", new []{ "a" }, null, 0, 9),
            new FloorSelection("b", new []{ "b" }, null, 0, 8),
            new FloorSelection("c", new []{ "c" }, null, 0, 7),
            new FloorSelection("d", new []{ "d" }, null, 0, 6),
            new FloorSelection("e", new []{ "e" }, null, 0, 5),
            new FloorSelection("f", new []{ "f" }, null, 0, 4),
            new FloorSelection("g", new []{ "g" }, null, 0, 3),
            new FloorSelection("g", new []{ "g" }, null, 0, 2),
            new FloorSelection("g", new []{ "g" }, null, 0, 1),
            new FloorSelection("ground", new []{ "ground" }, null, 0, 0),
            new FloorSelection("u", new []{ "u" }, null, 0, -1),
            new FloorSelection("u", new []{ "u" }, null, 0, -2),
        };

        [TestMethod]
        public void AssertThat_NumRef_FindsAllFloorsTagged()
        {
            TaggedRef i = new TaggedRef(new[] { "a" }, RefFilter.All, false);

            var matches = i.Match(2, _floors, null);

            Assert.AreEqual(3, matches.Count());
            Assert.IsTrue(matches.Contains(_floors[1]));
            Assert.IsTrue(matches.Contains(_floors[2]));
            Assert.IsTrue(matches.Contains(_floors[3]));
        }

        [TestMethod]
        public void AssertThat_NumRef_FindsAllFloorsTagged_WithAllTags()
        {
            TaggedRef i = new TaggedRef(new[] { "a", "x" }, RefFilter.All, false);

            var matches = i.Match(2, _floors, null);

            Assert.AreEqual(1, matches.Count());
            Assert.IsTrue(matches.Contains(_floors[2]));
        }
    }
}
