using System.Collections.Generic;
using Base_CityGeneration.Elements.Building.Design;
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
            new FloorSelection("top", new []{ new KeyValuePair<string, string>("tag", "top") }, null, null, 0, 12),
            new FloorSelection("a", new []{ new KeyValuePair<string, string>("tag", "a") }, null, null, 0, 11),
            new FloorSelection("a", new []{ new KeyValuePair<string, string>("tag", "a"), new KeyValuePair<string, string>("tag", "x") }, null, null, 0, 10),
            new FloorSelection("a", new []{ new KeyValuePair<string, string>("tag", "a") }, null, null, 0, 9),
            new FloorSelection("b", new []{ new KeyValuePair<string, string>("tag", "b") }, null, null, 0, 8),
            new FloorSelection("c", new []{ new KeyValuePair<string, string>("tag", "c") }, null, null, 0, 7),
            new FloorSelection("d", new []{ new KeyValuePair<string, string>("tag", "d") }, null, null, 0, 6),
            new FloorSelection("e", new []{ new KeyValuePair<string, string>("tag", "e") }, null, null, 0, 5),
            new FloorSelection("f", new []{ new KeyValuePair<string, string>("tag", "f") }, null, null, 0, 4),
            new FloorSelection("g", new []{ new KeyValuePair<string, string>("tag", "g") }, null, null, 0, 3),
            new FloorSelection("g", new []{ new KeyValuePair<string, string>("tag", "g") }, null, null, 0, 2),
            new FloorSelection("g", new []{ new KeyValuePair<string, string>("tag", "g") }, null, null, 0, 1),
            new FloorSelection("ground", new []{ new KeyValuePair<string, string>("tag", "ground") }, null, null, 0, 0),
            new FloorSelection("u", new []{ new KeyValuePair<string, string>("tag", "u") }, null, null, 0, -1),
            new FloorSelection("u", new []{ new KeyValuePair<string, string>("tag", "u") }, null, null, 0, -2),
        };

        [TestMethod]
        public void AssertThat_NumRef_FindsAllFloorsTagged()
        {
            TaggedRef i = new TaggedRef(new[] {
                new KeyValuePair<string, string>("tag", "a")
            }, RefFilter.All, false, false);

            var matches = i.Match(_floors, null);

            Assert.AreEqual(3, matches.Count());
            Assert.IsTrue(matches.Contains(_floors[1]));
            Assert.IsTrue(matches.Contains(_floors[2]));
            Assert.IsTrue(matches.Contains(_floors[3]));
        }

        [TestMethod]
        public void AssertThat_NumRef_FindsAllFloorsTagged_WithAllTags()
        {
            TaggedRef i = new TaggedRef(new[] {
                new KeyValuePair<string, string>("tag", "a"),
                new KeyValuePair<string, string>("tag", "x"),
            }, RefFilter.All, false, false);

            var matches = i.Match(_floors, null);

            Assert.AreEqual(1, matches.Count());
            Assert.IsTrue(matches.Contains(_floors[2]));
        }
    }
}
