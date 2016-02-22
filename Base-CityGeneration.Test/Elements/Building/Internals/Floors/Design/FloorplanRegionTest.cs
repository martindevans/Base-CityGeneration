//using System.Numerics;
//using Base_CityGeneration.Elements.Building.Internals.Floors.Design;
//using Microsoft.VisualStudio.TestTools.UnitTesting;

//namespace Base_CityGeneration.Test.Elements.Building.Internals.Floors.Design
//{
//    [TestClass]
//    public class FloorplanRegionTest
//    {
//        private readonly FloorplanRegion _region = new FloorplanRegion(new[] {
//            new FloorplanRegion.Side(new Vector2(-5, 5), new Vector2(5, 5), new Section[0]),
//            new FloorplanRegion.Side(new Vector2(5, 5), new Vector2(5, 0), new Section[0]),
//            new FloorplanRegion.Side(new Vector2(5, 0), new Vector2(5, -5), new Section[0]),
//            new FloorplanRegion.Side(new Vector2(5, -5), new Vector2(-5, -5), new Section[0]),
//        });

//        //[TestMethod]
//        //public void TestThat_IsNeighbour_DetectsNeighbours()
//        //{
//        //    FloorplanRegion r = new FloorplanRegion(new[] {
//        //        new Vector2(5, 0),
//        //        new Vector2(15, 0),
//        //        new Vector2(15, -5),
//        //        new Vector2(5, -5),
//        //    });

//        //    var n = _region.IsNeighbour(r);

//        //    Assert.IsTrue(n.HasValue);

//        //    Assert.AreEqual(_region, n.Value.Region);
//        //    Assert.AreEqual(2, n.Value.RegionOverlapIndex);

//        //    Assert.AreEqual(r, n.Value.Neighbour);
//        //    Assert.AreEqual(3, n.Value.NeighbourOverlapIndex);
//        //}

//        //[TestMethod]
//        //public void TestThat_IsNeighbour_DetectsNonNeighbours()
//        //{
//        //    FloorplanRegion r = new FloorplanRegion(new[] {
//        //        new Vector2(6, 0),
//        //        new Vector2(15, 0),
//        //        new Vector2(15, -5),
//        //        new Vector2(6, -5),
//        //    });

//        //    var n = _region.IsNeighbour(r);

//        //    Assert.IsFalse(n.HasValue);
//        //}

//        //[TestMethod]
//        //public void TestThat_Merge_MergesShapes_WithTwoOverlappingPoints()
//        //{
//        //    FloorplanRegion r = new FloorplanRegion(new[] {
//        //        new Vector2(5, 0),
//        //        new Vector2(15, 0),
//        //        new Vector2(15, -5),
//        //        new Vector2(5, -5),
//        //    });

//        //    var neighbour = _region.IsNeighbour(r);

//        //    Assert.IsTrue(neighbour.HasValue);

//        //    var result = FloorplanRegion.Merge(neighbour.Value);

//        //    Assert.AreEqual(7, result.Shape.Count);
//        //    Assert.AreEqual(new Vector2(-5, 5), result.Shape[0]);
//        //    Assert.AreEqual(new Vector2(5, 5), result.Shape[1]);
//        //    Assert.AreEqual(new Vector2(5, 0), result.Shape[2]);
//        //    Assert.AreEqual(new Vector2(15, 0), result.Shape[3]);
//        //    Assert.AreEqual(new Vector2(15, -5), result.Shape[4]);
//        //    Assert.AreEqual(new Vector2(5, -5), result.Shape[5]);
//        //    Assert.AreEqual(new Vector2(-5, -5), result.Shape[6]);
//        //}

//        //[TestMethod]
//        //public void TestThat_Merge_MergesShapes_WithManyOverlappingPoints()
//        //{
//        //    FloorplanRegion r = new FloorplanRegion(new[] {
//        //        new Vector2(5, -5),
//        //        new Vector2(5, 0),
//        //        new Vector2(5, 5),
//        //        new Vector2(15, 5),
//        //        new Vector2(15, -5),
//        //    });

//        //    var neighbour = _region.IsNeighbour(r);

//        //    Assert.IsTrue(neighbour.HasValue);

//        //    var result = FloorplanRegion.Merge(neighbour.Value);

//        //    Assert.AreEqual(6, result.Shape.Count);
//        //    Assert.AreEqual(new Vector2(-5, 5), result.Shape[0]);
//        //    Assert.AreEqual(new Vector2(5, 5), result.Shape[1]);
//        //    Assert.AreEqual(new Vector2(15, 5), result.Shape[2]);
//        //    Assert.AreEqual(new Vector2(15, -5), result.Shape[3]);
//        //    Assert.AreEqual(new Vector2(5, -5), result.Shape[4]);
//        //    Assert.AreEqual(new Vector2(-5, -5), result.Shape[5]);
//        //}
//    }
//}
