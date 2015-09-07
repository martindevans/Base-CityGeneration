﻿using Base_CityGeneration.Elements.Building.Design;
using Base_CityGeneration.Elements.Building.Design.Spec;
using Base_CityGeneration.Elements.Building.Design.Spec.Ref;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Base_CityGeneration.Test.Elements.Building.Design.Spec.Ref
{
    [TestClass]
    public class TaggedRefTest
    {
        readonly FloorSelection[] _floors = {
            new FloorSelection("top", new []{ "top" }, null, null, 0, 12),
            new FloorSelection("a", new []{ "a" }, null, null, 0, 11),
            new FloorSelection("a", new []{ "a", "x" }, null, null, 0, 10),
            new FloorSelection("a", new []{ "a" }, null, null, 0, 9),
            new FloorSelection("b", new []{ "b" }, null, null, 0, 8),
            new FloorSelection("c", new []{ "c" }, null, null, 0, 7),
            new FloorSelection("d", new []{ "d" }, null, null, 0, 6),
            new FloorSelection("e", new []{ "e" }, null, null, 0, 5),
            new FloorSelection("f", new []{ "f" }, null, null, 0, 4),
            new FloorSelection("g", new []{ "g" }, null, null, 0, 3),
            new FloorSelection("g", new []{ "g" }, null, null, 0, 2),
            new FloorSelection("g", new []{ "g" }, null, null, 0, 1),
            new FloorSelection("ground", new []{ "ground" }, null, null, 0, 0),
            new FloorSelection("u", new []{ "u" }, null, null, 0, -1),
            new FloorSelection("u", new []{ "u" }, null, null, 0, -2),
        };

        [TestMethod]
        public void AssertThat_NumRef_FindsAllFloorsTagged()
        {
            TaggedRef i = new TaggedRef(new[] { "a" }, RefFilter.All, false, false);

            var matches = i.Match(2, _floors, null);

            Assert.AreEqual(3, matches.Count());
            Assert.IsTrue(matches.Contains(_floors[1]));
            Assert.IsTrue(matches.Contains(_floors[2]));
            Assert.IsTrue(matches.Contains(_floors[3]));
        }

        [TestMethod]
        public void AssertThat_NumRef_FindsAllFloorsTagged_WithAllTags()
        {
            TaggedRef i = new TaggedRef(new[] { "a", "x" }, RefFilter.All, false, false);

            var matches = i.Match(2, _floors, null);

            Assert.AreEqual(1, matches.Count());
            Assert.IsTrue(matches.Contains(_floors[2]));
        }
    }
}
