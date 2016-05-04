using System.Collections.Generic;
using Base_CityGeneration.Elements.Building.Design;
using Base_CityGeneration.Elements.Building.Design.Spec.Ref;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using EpimetheusPlugins.Scripts;

namespace Base_CityGeneration.Test.Elements.Building.Design.Spec.Ref
{
    [TestClass]
    public class BaseRefTest
    {
        private static FloorSelection CreateFloor(string name, float height, int number)
        {
            return new FloorSelection(name, new KeyValuePair<string, string>[0], new ScriptReference(typeof(TestScript)), height, number);
        }

        private readonly FloorSelection[] _floors = {
            CreateFloor("roof", 0, 12),

            CreateFloor("penthouse", 0, 13),

            CreateFloor("skylobby", 0, 12),
            CreateFloor("residential", 0, 11),
            CreateFloor("residential", 0, 10),
            CreateFloor("residential", 0, 9),

            CreateFloor("skylobby", 0, 8),
            CreateFloor("residential", 0, 7),
            CreateFloor("residential", 0, 6),
            CreateFloor("residential", 0, 5),

            CreateFloor("skylobby", 0, 4),
            CreateFloor("residential", 0, 3),
            CreateFloor("residential", 0, 2),
            CreateFloor("residential", 0, 1),

            CreateFloor("ground", 0, 0),

            CreateFloor("basement", 0, -1),
            CreateFloor("basement", 0, -2),
        };

        [TestMethod]
        public void AssertThat_BaseRef_MatchFrom_SelectsNonOverlappingSections_WhenNonOverlappingModeIsSpecified()
        {
            IdRef residential = new IdRef("residential", SearchDirection.Up, RefFilter.All, false, false);
            IdRef skylobby = new IdRef("skylobby", SearchDirection.Up, RefFilter.First, true, false);

            var res = residential.Match(_floors, null).ToArray();
            var matched = skylobby.MatchFrom(_floors, residential, res).ToArray();

            //We should have matched all the residential floors, paired with the next skylobby up
            //Then filtered these floors into groups which overlap, and take the first (i.e. lowest floor to next skylobby)
            Assert.AreEqual(3, matched.Count());
            Assert.IsTrue(matched.Any(a => a.Key.Index == 1 && a.Value.Index == 4));
            Assert.IsTrue(matched.Any(a => a.Key.Index == 5 && a.Value.Index == 8));
            Assert.IsTrue(matched.Any(a => a.Key.Index == 9 && a.Value.Index == 12));
        }

        [TestMethod]
        public void AssertThat_BaseRef_SelectsFirstSections_WhenFilterFirstIsSpecified()
        {
            IdRef bot = new IdRef("skylobby", SearchDirection.Up, RefFilter.All, false, false);
            IdRef top = new IdRef("skylobby", SearchDirection.Up, RefFilter.First, false, false);

            var b = bot.Match(_floors, null).ToArray();
            var matched = top.MatchFrom(_floors, bot, b).ToArray();

            Assert.AreEqual(2, matched.Count());
            Assert.IsTrue(matched.Any(a => a.Key.Index == 4 && a.Value.Index == 8));
            Assert.IsTrue(matched.Any(a => a.Key.Index == 8 && a.Value.Index == 12));
        }
    }
}
