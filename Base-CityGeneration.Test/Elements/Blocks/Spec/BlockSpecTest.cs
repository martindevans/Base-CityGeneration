using Base_CityGeneration.Elements.Blocks.Spec;
using Base_CityGeneration.Elements.Blocks.Spec.Subdivision;
using Base_CityGeneration.Elements.Blocks.Spec.Subdivision.Rules;
using Base_CityGeneration.Parcels.Parcelling;
using Base_CityGeneration.Test.Elements.Building.Internals.Floors.Floors.Selection;
using EpimetheusPlugins.Testing.MockScripts;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using System;
using System.IO;
using System.Linq;

namespace Base_CityGeneration.Test.Elements.Blocks.Spec
{
    [TestClass]
    public class BlockSpecTest
    {
        readonly BlockSpec _spec = BlockSpec.Deserialize(new StringReader(@"
!Block
Aliases: []

# How should we split this block up into parcels?
Subdivide: !ObbParceller
    Rules:
        # Min/Max area a parcels must be (chance that the max will be exceeded)
        - !Area
          Min: 250
          Max: 1000
          TerminationChance: 0.25
        # Min/Max frontage a parcels must have (chance that max will be exceeded)
        - !Frontage
          Min: 25
          Max: 500
          TerminationChance: 0.45
          Type: road
        # Resource which the parcels must have access to (chance that it will not have access)
        - !Access
          Type: road
          TerminationChance: 0.15

# Tags for what lots will be placed into parcels
Lots:
    # What constraints must be met to use this set of tags?
    - Constraints:
        - !RequireArea { Min: 10, Max: 20 }
      Tags:
        1: [Overconstrained]

    - Constraints:
        - !RequireArea { Min: 300 }
        - !RequireAccess { Type: road }
      Tags:
        1: [Selection1]

    # No constraints. Selection proceeds from top to bottom, so in this case that means no road access
    - Tags:
        1: [Selection2]
"));

        [TestMethod]
        public void AssertThat_BlockSpec_CanBeDeserialized()
        {
            //Check subdivider spec
            Assert.IsTrue(_spec.Subdivider is ObbParcellerSpec);
            Assert.AreEqual(3, _spec.Subdivider.Rules.Count());
            Assert.AreEqual(1, _spec.Subdivider.Rules.OfType<AreaRuleSpec>().Count());
            Assert.AreEqual(1, _spec.Subdivider.Rules.OfType<FrontageRuleSpec>().Count());
            Assert.AreEqual(1, _spec.Subdivider.Rules.OfType<AccessRuleSpec>().Count());

            //Check lot specs
            Assert.AreEqual(3, _spec.Lots.Count());
            Assert.AreEqual(2, _spec.Lots.Skip(1).First().Constraints.Count());
        }

        [TestMethod]
        public void AssertThat_BlockSpec_SelectsTightestConstraintsForParcel()
        {
            Parcel p = new Parcel(new Parcel.Edge[] {
                new Parcel.Edge { Start = new Vector2(0, 0), End = new Vector2(100, 0), Resources = new [] { "road" } },
                new Parcel.Edge { Start = new Vector2(100, 0), End = new Vector2(100, -100), Resources = new [] { "road" } },
                new Parcel.Edge { Start = new Vector2(100, -100), End = new Vector2(0, -100), Resources = new [] { "road" } },
                new Parcel.Edge { Start = new Vector2(0, -100), End = new Vector2(0, 0), Resources = new [] { "road" } },
            });

            var selected = _spec.SelectLot(p, () => 1, a => ScriptReferenceFactory.Create(typeof(TestScript), Guid.NewGuid(), string.Join(",", a)));

            Assert.AreEqual("Selection1", selected.Name);
        }

        [TestMethod]
        public void AssertThat_BlockSpec_GeneratesParcels()
        {
            Parcel root = new Parcel(new Parcel.Edge[] {
                new Parcel.Edge { Start = new Vector2(0, 0), End = new Vector2(500, 0), Resources = new [] { "road" } },
                new Parcel.Edge { Start = new Vector2(500, 0), End = new Vector2(500, -500), Resources = new [] { "road" } },
                new Parcel.Edge { Start = new Vector2(500, -500), End = new Vector2(0, -500), Resources = new [] { "road" } },
                new Parcel.Edge { Start = new Vector2(0, -500), End = new Vector2(0, 0), Resources = new [] { "road" } },
            });

            Random r = new Random(1);
            var parcels = _spec.CreateParcels(root, r.NextDouble);

            //Check that the minimum area spec has not been violated
            Assert.IsTrue(parcels.All(a => a.Area() >= 250));

            //Check that the minimum frontage spec has not been violated (*if* the parcel has a frontage)
            Assert.IsTrue(parcels.All(a => !a.HasAccess("road") || a.MaxAccessFrontage("road") >= 25));
        }
    }
}
