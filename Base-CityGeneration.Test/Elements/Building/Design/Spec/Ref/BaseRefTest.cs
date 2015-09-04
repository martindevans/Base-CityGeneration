using Base_CityGeneration.Elements.Building.Design.Spec;
using Base_CityGeneration.Elements.Building.Design.Spec.Ref;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Base_CityGeneration.Test.Elements.Building.Design.Spec.Ref
{
    [TestClass]
    public class BaseRefTest
    {
        private readonly FloorSelection[] _floors = {
            new FloorSelection("roof", null, null, 0, 12),

            new FloorSelection("penthouse", null, null, 0, 13),

            new FloorSelection("skylobby", null, null, 0, 12),
            new FloorSelection("residential", null, null, 0, 11),
            new FloorSelection("residential", null, null, 0, 10),
            new FloorSelection("residential", null, null, 0, 9),

            new FloorSelection("skylobby", null, null, 0, 8),
            new FloorSelection("residential", null, null, 0, 7),
            new FloorSelection("residential", null, null, 0, 6),
            new FloorSelection("residential", null, null, 0, 5),

            new FloorSelection("skylobby", null, null, 0, 4),
            new FloorSelection("residential", null, null, 0, 3),
            new FloorSelection("residential", null, null, 0, 2),
            new FloorSelection("residential", null, null, 0, 1),

            new FloorSelection("ground", null, null, 0, 0),

            new FloorSelection("basement", null, null, 0, -1),
            new FloorSelection("basement", null, null, 0, -2),
        };

        [TestMethod]
        public void AssertThat_BaseRef_MatchFrom_SelectsNonOverlappingSections_WhenNonOverlappingModeIsSpecified()
        {
            IdRef residential = new IdRef("residential", SearchDirection.Up, RefFilter.All, false, false);
            IdRef skylobby = new IdRef("skylobby", SearchDirection.Up, RefFilter.First, true, false);

            var res = residential.Match(2, _floors, null).ToArray();
            var matched = skylobby.MatchFrom(2, _floors, residential, res).ToArray();

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

            var b = bot.Match(2, _floors, null).ToArray();
            var matched = top.MatchFrom(2, _floors, bot, b).ToArray();

            Assert.AreEqual(2, matched.Count());
            Assert.IsTrue(matched.Any(a => a.Key.Index == 4 && a.Value.Index == 8));
            Assert.IsTrue(matched.Any(a => a.Key.Index == 8 && a.Value.Index == 12));
        }
    }
}
