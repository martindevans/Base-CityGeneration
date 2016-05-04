using System.Collections.Generic;
using Base_CityGeneration.Elements.Building.Design;
using Base_CityGeneration.Elements.Building.Design.Spec.Ref;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using EpimetheusPlugins.Scripts;

namespace Base_CityGeneration.Test.Elements.Building.Design.Spec.Ref
{
    [TestClass]
    public class TaggedRefTest
    {
        private static FloorSelection CreateFloor(string name, float height, int number, params KeyValuePair<string, string>[] tags)
        {
            return new FloorSelection(
                name,
                tags,
                new ScriptReference(typeof(TestScript)),
                height,
                number
            );
        }

        readonly FloorSelection[] _floors = {
            CreateFloor("top", 0, 12, new KeyValuePair<string, string>("tag", "top")),
            CreateFloor("a", 0, 11, new KeyValuePair<string, string>("tag", "a")),
            CreateFloor("a", 0, 10, new KeyValuePair<string, string>("tag", "a"), new KeyValuePair<string, string>("tag", "x")),
            CreateFloor("a", 0, 9, new KeyValuePair<string, string>("tag", "a")),
            CreateFloor("b", 0, 8, new KeyValuePair<string, string>("tag", "b")),
            CreateFloor("c", 0, 7, new KeyValuePair<string, string>("tag", "c")),
            CreateFloor("d", 0, 6, new KeyValuePair<string, string>("tag", "d")),
            CreateFloor("e", 0, 5, new KeyValuePair<string, string>("tag", "e")),
            CreateFloor("f", 0, 4, new KeyValuePair<string, string>("tag", "f")),
            CreateFloor("g", 0, 3, new KeyValuePair<string, string>("tag", "g")),
            CreateFloor("g", 0, 2, new KeyValuePair<string, string>("tag", "g")),
            CreateFloor("g", 0, 1, new KeyValuePair<string, string>("tag", "g")),
            CreateFloor("ground", 0, 0, new KeyValuePair<string, string>("tag", "ground")),
            CreateFloor("u", 0, -1, new KeyValuePair<string, string>("tag", "u")),
            CreateFloor("u", 0, -2, new KeyValuePair<string, string>("tag", "u")),
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
