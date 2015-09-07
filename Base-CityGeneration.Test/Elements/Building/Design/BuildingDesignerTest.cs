using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Base_CityGeneration.Elements.Building.Design;
using Base_CityGeneration.Elements.Building.Design.Spec.Markers;
using EpimetheusPlugins.Scripts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Base_CityGeneration.Test.Elements.Building.Design
{
    [TestClass]
    public class BuildingDesignerTest
    {
        private static ScriptReference Finder(string[] tags)
        {
            Assert.IsNotNull(tags);
            return new ScriptReference(typeof(TestScript));
        }

        [TestMethod]
        public void AssertThat_SingleFloorBuilding_OutputsSingleFloor()
        {
            var b = BuildingDesigner.Deserialize(new StringReader(@"
!Building
Verticals: []
Floors:
    - !Floor
      Height:
        !UniformValue
        Min: 1
        Max: 2
      Tags:
        1: [a]
    - !Ground []
"));

            Assert.IsNotNull(b);

            Random r = new Random();
            var selection = b.Internals(r.NextDouble, null, Finder);

            Assert.AreEqual(1, selection.AboveGroundFloors.Count());
            Assert.AreEqual(0, selection.BelowGroundFloors.Count());

            var h = selection.AboveGroundFloors.Single().Height;
            Assert.IsTrue(h >= 1 && h <= 2);
        }

        [TestMethod]
        public void AssertThat_TwoFloors_WithHeightGroup_InheritsHeightFromRootGroup()
        {
            var b = BuildingDesigner.Deserialize(new StringReader(@"
!Building
Aliases:
    - &groupname !NormalValue
      Vary: false
      Min: 5
      Max: 10
Verticals: []
Floors:
    - !Floor
      Height: *groupname
      Tags:
        1: [a]
    - !Floor
      Height: *groupname
      Tags:
        1: [a]
    - !Ground []
"));

            Assert.IsNotNull(b);

            Random r = new Random();
            var selection = b.Internals(r.NextDouble, null, Finder);

            Assert.AreEqual(2, selection.AboveGroundFloors.Count());
            Assert.AreEqual(0, selection.BelowGroundFloors.Count());

            var h = selection.AboveGroundFloors.First().Height;
            Assert.IsTrue(h >= 5f && h <= 10f);

            var h2 = selection.AboveGroundFloors.Skip(1).First().Height;
            Assert.AreEqual(h, h2);
        }

        [TestMethod]
        public void AssertThat_SingleFloorBuilding_WithNullFloor_OutputsNoFloors()
        {
            var b = BuildingDesigner.Deserialize(new StringReader(@"
!Building
Verticals: []
Floors:
    - !Floor
      Tags:
        1: null
    - !Ground []
"));

            Assert.IsNotNull(b);

            Random r = new Random();
            var selection = b.Internals(r.NextDouble, null, Finder);

            Assert.AreEqual(0, selection.AboveGroundFloors.Count());
            Assert.AreEqual(0, selection.BelowGroundFloors.Count());
        }

        [TestMethod]
        public void AssertThat_MultiFloorSelector_OutputsMultipleFloors()
        {
            var b = BuildingDesigner.Deserialize(new StringReader(@"
!Building
Verticals: []
Floors:
    - !Range
      Includes:
        - Count:
            !UniformValue
            Min: 1
            Max: 5
          Vary: true
          Tags:
            1: [a]
            1: [b]
            0: null
    - !Ground []
"));

            Assert.IsNotNull(b);

            Random r = new Random();
            var selection = b.Internals(r.NextDouble, null, Finder);

            Assert.IsTrue(selection.AboveGroundFloors.Any() && selection.AboveGroundFloors.Count() <= 5);
            Assert.AreEqual(0, selection.BelowGroundFloors.Count());
        }

        [TestMethod]
        public void AssertThat_FacadeSelector_OutputsFacade()
        {
            var b = BuildingDesigner.Deserialize(new StringReader(@"
!Building
Verticals: []
Facades:
    - Tags: { 1: [test] }
      Bottom: !Num { N: 0 }
      Top: !Id { Id: Top, Search: Up, Filter: Longest, NonOverlapping: true }
Floors:
    - !Floor { Id: Top, Tags: { 1: [a] } }
    - !Floor { Id: F, Tags: { 1: [a] } }
    - !Floor { Id: F, Tags: { 1: [a] } }
    - !Floor { Id: F, Tags: { 1: [a] } }
    - !Floor { Id: Bot, Tags: { 1: [a] } }
    - !Ground []
"));

            Assert.IsNotNull(b);

            Random r = new Random(2);
            var selection = b.Internals(r.NextDouble, null, Finder).Externals(r.NextDouble, null, Finder, new [] { 0f });

            Assert.AreEqual(1, selection.Facades.Count());
            Assert.AreEqual(1, selection.Facades.Single().Count());
        }

        [TestMethod]
        [ExpectedException(typeof(DesignFailedException))]
        public void AssertThat_FacadeSelector_Throws_WhenNotAllFloorsAreCovered()
        {
            var b = BuildingDesigner.Deserialize(new StringReader(@"
!Building
Verticals: []
Facades:
    - Tags: { 1: [test] }
      Bottom: !Num { N: 0 }
      Top: !Id { Id: F, Search: Up, Filter: Shortest, NonOverlapping: true }    #Matching to the *first* F does not cover all floors!
Floors:
    - !Floor { Id: Top, Tags: { 1: [a] } }
    - !Floor { Id: F, Tags: { 1: [a] } }
    - !Floor { Id: F, Tags: { 1: [a] } }
    - !Floor { Id: F, Tags: { 1: [a] } }
    - !Floor { Id: Bot, Tags: { 1: [a] } }
    - !Ground []
"));

            Assert.IsNotNull(b);

            Random r = new Random(2);
            b.Internals(r.NextDouble, null, Finder).Externals(r.NextDouble, null, Finder, new ReadOnlyCollection<float>(new float[] { 0 }));
        }

        [TestMethod]
        public void AssertThat_FloorSelector_SelectsGroundMarkerAsFootprint()
        {
            var b = BuildingDesigner.Deserialize(new StringReader(@"
!Building
Verticals: []
Facades:
    - Tags: { 1: [test] }
      Bottom: !Num { N: 0 }
      Top: !Id { Id: Top, Search: Up, Filter: Longest, NonOverlapping: true }
Floors:
    - !Floor { Id: Top, Tags: { 1: [a] } }
    - !Floor { Id: F, Tags: { 1: [a] } }
    - !Floor { Id: F, Tags: { 1: [a] } }
    - !Floor { Id: F, Tags: { 1: [a] } }
    - !Floor { Id: Bot, Tags: { 1: [a] } }
    - !Ground []
"));

            Assert.IsNotNull(b);

            Random r = new Random(2);
            var selection = b.Internals(r.NextDouble, null, Finder).Externals(r.NextDouble, null, Finder, new[] { 0f });

            Assert.AreEqual(1, selection.Footprints.Count());
            Assert.AreEqual(0, selection.Footprints.Single().Index);
            Assert.IsInstanceOfType(selection.Footprints.Single().Marker, typeof(GroundMarker));
        }

        [TestMethod]
        public void AssertThat_FloorSelector_SelectsNextMarkerDownAsFootprint()
        {
            var b = BuildingDesigner.Deserialize(new StringReader(@"
!Building
Verticals: []
Floors:
    - !Floor { Id: F, Tags: { 1: [a] } }
    - !Floor { Id: F, Tags: { 1: [a] } }
    - !Footprint []
    - !Floor { Id: F, Tags: { 1: [a] } }
    - !Floor { Id: F, Tags: { 1: [a] } }
    - !Ground []
"));

            Assert.IsNotNull(b);

            Random r = new Random(2);
            var selection = b.Internals(r.NextDouble, null, Finder);

            Assert.AreEqual(2, selection.Footprints.Count());
            Assert.IsTrue(selection.Footprints.Any(a => a.Index == 0));
            Assert.IsTrue(selection.Footprints.Any(a => a.Index == 2));
            Assert.IsTrue(selection.Footprints.Any(a => a.Marker is GroundMarker));
            Assert.IsTrue(selection.Footprints.Any(a => a.Marker is FootprintMarker));
        }

        [TestMethod]
        public void AssertThat_FacadeSelector_OutputsFacades()
        {
            var b = BuildingDesigner.Deserialize(new StringReader(@"
!Building
Verticals: []
Facades:
    - Tags: { 1: [b] }
      Bottom: !Id { Id: F }
      Top: !Id { Id: F, Search: Up, Filter: Longest, NonOverlapping: true }

    - Tags: { 1: [a] }
      Bottom: !Num { N: 0 }
      Top: !Num { N: 0, Inclusive: true }

    - Tags: { 1: [c] }
      Bottom: !Id { Id: Top }
      Top: !Id { Id: Top, Inclusive: true }
Floors:
    - !Floor { Id: Top, Tags: { 1: [a] } }
    - !Floor { Id: F, Tags: { 1: [a] } }
    - !Floor { Id: F, Tags: { 1: [a] } }
    - !Floor { Id: F, Tags: { 1: [a] } }
    - !Floor { Id: Bot, Tags: { 1: [a] } }
    - !Ground []
"));

            Assert.IsNotNull(b);

            Random r = new Random(2);
            var selection = b.Internals(r.NextDouble, null, Finder).Externals(r.NextDouble, null, Finder, new float[] { 0 });

            Assert.AreEqual(1, selection.Facades.Count());
            Assert.AreEqual(3, selection.Facades.Single().Count());
            Assert.IsTrue(selection.Facades.Single().Any(f => f.Bottom == 0 && f.Top == 0));
            Assert.IsTrue(selection.Facades.Single().Any(f => f.Bottom == 1 && f.Top == 3));
            Assert.IsTrue(selection.Facades.Single().Any(f => f.Bottom == 4 && f.Top == 4));
        }

        [TestMethod]
        public void AssertThat_FacadeSelector_OutputsFacades_OnlyForNonObscuredFloors()
        {
            var b = BuildingDesigner.Deserialize(new StringReader(@"
!Building
Verticals: []
Facades:
    - Tags: { 1: [b] }
      Bottom: !Id { Id: F }
      Top: !Id { Id: F, Search: Up, Filter: Longest, NonOverlapping: true }

    - Tags: { 1: [a] }
      Bottom: !Num { N: 0 }
      Top: !Num { N: 0, Inclusive: true }

    - Tags: { 1: [c] }
      Bottom: !Id { Id: Top }
      Top: !Id { Id: Top, Inclusive: true }
Floors:
    - !Floor { Id: Top, Tags: { 1: [a] }, Height: 1 }
    - !Floor { Id: F, Tags: { 1: [a] }, Height: 1 }
    - !Floor { Id: F, Tags: { 1: [a] }, Height: 1 }
    - !Floor { Id: F, Tags: { 1: [a] }, Height: 1 }
    - !Floor { Id: Bot, Tags: { 1: [a] }, Height: 1 }
    - !Ground []
"));

            Assert.IsNotNull(b);

            Random r = new Random(2);
            var selection = b.Internals(r.NextDouble, null, Finder).Externals(r.NextDouble, null, Finder, new float[] { 1 });

            Assert.AreEqual(1, selection.Facades.Count());
            Assert.AreEqual(2, selection.Facades.Single().Count());
            Assert.IsTrue(selection.Facades.Single().Any(f => f.Bottom == 1 && f.Top == 3));
            Assert.IsTrue(selection.Facades.Single().Any(f => f.Bottom == 4 && f.Top == 4));
        }

        [TestMethod]
        public void AssertThat_FacadeSelector_DoesNotOutputFacadeAcrossFootprintBreak()
        {
            var b = BuildingDesigner.Deserialize(new StringReader(@"
!Building
Verticals: []
Facades:
    - Tags: { 1: [b] }
      Bottom: !Id { Id: F }
      Top: !Id { Id: F, Search: Up, Filter: Longest, NonOverlapping: true }

Floors:
    - !Floor { Id: F, Tags: { 1: [a] }, Height: 1 }
    - !Floor { Id: F, Tags: { 1: [a] }, Height: 1 }
    - !Footprint []
    - !Floor { Id: F, Tags: { 1: [a] }, Height: 1 }
    - !Floor { Id: F, Tags: { 1: [a] }, Height: 1 }
    - !Ground []
"));

            Assert.IsNotNull(b);

            Random r = new Random(2);
            var selection = b.Internals(r.NextDouble, null, Finder).Externals(r.NextDouble, null, Finder, new float[] { 0 });

            Assert.AreEqual(1, selection.Facades.Count());
            Assert.AreEqual(2, selection.Facades.Single().Count());
            Assert.IsTrue(selection.Facades.Single().Any(f => f.Bottom == 0 && f.Top == 1));
            Assert.IsTrue(selection.Facades.Single().Any(f => f.Bottom == 2 && f.Top == 3));
        }

        [TestMethod]
        public void Playground()
        {
            var b = BuildingDesigner.Deserialize(new StringReader(@"
!Building
Verticals:
    - Tags: { 1: [lift] }
      Bottom: !Id { Id: GroundFloor }
      Top: !Id { Id: TopFloor }
Floors:
    - !Floor { Id: TopFloor, Tags: { 1: [a] } }
    - !Floor { Tags: { 1: [a] } }
    - !Floor { Id: GroundFloor, Tags: { 1: [a] } }
    - !Ground []
"));

            Func<string[], ScriptReference> finder = tags => {
                Assert.IsNotNull(tags);
                return new ScriptReference(typeof(TestScript));
            };

            Random r = new Random();
            var selection = b.Internals(r.NextDouble, null, finder);

            Assert.AreEqual(3, selection.AboveGroundFloors.Count());
            Assert.AreEqual(1, selection.Verticals.Count());
        }
    }

    [Script("93863CB4-2951-453E-95B8-955077322550", "Test")]
    internal class TestScript
    {
    }
}
