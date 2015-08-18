using Base_CityGeneration.Elements.Roads.Hyperstreamline;
using Base_CityGeneration.Elements.Roads.Hyperstreamline.Tracing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using System;
using System.IO;
using System.Linq;
using Myre.Collections;

namespace Base_CityGeneration.Test.Elements.Roads.Hyperstreamline
{
    [TestClass]
    public class NetworkBuilderTest
    {
        [TestMethod]
        public void NetworkBuilder_GeneratesMajorRoads()
        {
            var c = NetworkDescriptor.Deserialize(new StringReader(@"
!Network
Major:
    MergeSearchAngle: 22.5
    MergeDistance: 25
    SegmentLength: 10
    RoadWidth: !NormalValue { Min: 1, Max: 10, Vary: true }
    PriorityField: !ConstantScalars { Value: 1 }
    SeparationField: !ConstantScalars { Value: 50 }
    TensorField:
        !AddTensors
        Tensors:
            - !PointDistanceDecayTensors { Tensors: !Grid { Angle: 0, Length: 1 }, Decay: 2.5, Center: { X: 0, Y: 0 } }
            - !PointDistanceDecayTensors { Tensors: !Grid { Angle: 45, Length: 1 }, Decay: 2.5, Center: { X: 1, Y: 0 } }
            - !PointDistanceDecayTensors { Tensors: !Grid { Angle: 45, Length: 1 }, Decay: 2.5, Center: { X: 0, Y: 1 } }
            - !PointDistanceDecayTensors { Tensors: !Grid { Angle: 0, Length: 1 }, Decay: 2.5, Center: { X: 1, Y: 1 } }
"));

            NetworkBuilder b = new NetworkBuilder();

            Random r = new Random(10);
            var m = new NamedBoxCollection();

            b.Build(c.Major(r.NextDouble, m), r.NextDouble, m, new Vector2(0, 0), new Vector2(500, 500));
            b.Reduce();

            Console.WriteLine(b.Result.ToSvg());
        }

        [TestMethod]
        public void NetworkBuilder_GeneratesRegions()
        {
            var c = NetworkDescriptor.Deserialize(new StringReader(@"
!Network
Major:
    MergeSearchAngle: 22.5
    MergeDistance: 25
    SegmentLength: 10
    RoadWidth: !NormalValue { Min: 1, Max: 10, Vary: true }
    PriorityField: !ConstantScalars { Value: 1 }
    SeparationField: !ConstantScalars { Value: 50 }
    TensorField:
        !AddTensors
        Tensors:
            - !PointDistanceDecayTensors { Tensors: !Grid { Angle: 0, Length: 1 }, Decay: 2.5, Center: { X: 0, Y: 0 } }
            - !PointDistanceDecayTensors { Tensors: !Grid { Angle: 45, Length: 1 }, Decay: 2.5, Center: { X: 1, Y: 0 } }
            - !PointDistanceDecayTensors { Tensors: !Grid { Angle: 45, Length: 1 }, Decay: 2.5, Center: { X: 0, Y: 1 } }
            - !PointDistanceDecayTensors { Tensors: !Grid { Angle: 0, Length: 1 }, Decay: 2.5, Center: { X: 1, Y: 1 } }
"));

            NetworkBuilder b = new NetworkBuilder();

            Random r = new Random(10);
            var m = new NamedBoxCollection();

            b.Build(c.Major(r.NextDouble, m), r.NextDouble, m, new Vector2(0, 0), new Vector2(500, 500));
            b.Reduce();

            var regions = b.Regions();

            Console.WriteLine(b.Result.ToSvg(regions));
        }

        [TestMethod]
        public void NetworkBuilder_GeneratesMinorRoads()
        {
            var c = NetworkDescriptor.Deserialize(new StringReader(@"
!Network
Aliases:
    - &base-field !AddTensors
      Tensors:
        - !PointDistanceDecayTensors { Tensors: !Grid { Angle: 0 }, Decay: 2.5, Center: { X: 0, Y: 0 } }
        - !PointDistanceDecayTensors { Tensors: !Grid { Angle: 45 }, Decay: 2.5, Center: { X: 1, Y: 0 } }
        - !PointDistanceDecayTensors { Tensors: !Grid { Angle: 45 }, Decay: 2.5, Center: { X: 0, Y: 1 } }
        - !PointDistanceDecayTensors { Tensors: !Grid { Angle: 0 }, Decay: 2.5, Center: { X: 1, Y: 1 } }

Major:
    MergeSearchAngle: 22.5
    MergeDistance: 25
    SegmentLength: 10
    RoadWidth: !NormalValue { Min: 2, Max: 4, Vary: true }
    PriorityField: !ConstantScalars { Value: 1 }
    SeparationField: !ConstantScalars { Value: 50 }
    TensorField: *base-field

Minor:
    MergeSearchAngle: 12.5
    MergeDistance: 2.5
    SegmentLength: 2
    RoadWidth: !NormalValue { Min: 1, Max: 2, Vary: true }
    PriorityField: !ConstantScalars { Value: 1 }
    SeparationField: !ConstantScalars { Value: 25 }
    TensorField:
        !WeightedAverage
        Tensors:
            0.1: *base-field
            0.9: !Grid { Angle: !UniformValue { Min: 1, Max: 360, Vary: true } }
"));

            Random r = new Random(12);
            NetworkBuilder b = new NetworkBuilder();
            var m = new NamedBoxCollection();

            b.Build(c.Major(r.NextDouble, m), r.NextDouble, m, new Vector2(0, 0), new Vector2(100, 100));
            b.Reduce();

            var regions = b.Regions();
            foreach (var region in regions)
                b.Build(c.Minor(r.NextDouble, m), r.NextDouble, m, region);

            Console.WriteLine(b.Result.ToSvg());

            Assert.IsFalse(b.Result.Vertices.GroupBy(a => a.Position).Any(a => a.Count() > 1));

        }

        [TestMethod]
        public void NetworkBuilder_CanHaveRngEverywhere()
        {
            var c = NetworkDescriptor.Deserialize(new StringReader(@"
!Network
Aliases:
    - &base-field !AddTensors
      Tensors:
        - !PointDistanceDecayTensors {
            Tensors: !Grid { Angle: !ConstantValue { Value: 0 } },
            Decay: !ConstantValue { Value: 2.5 },
            Center: { X: !ConstantValue { Value: 0 }, Y: !ConstantValue { Value: 0 } }
        }

Major:
    MergeSearchAngle: !ConstantValue { Value: 22.5 }
    MergeDistance: !ConstantValue { Value: 25 }
    SegmentLength: !ConstantValue { Value: 10 }
    RoadWidth: !NormalValue { Min: 2, Max: 4, Vary: true }
    PriorityField: !ConstantScalars { Value: 1 }
    SeparationField: !ConstantScalars { Value: 50 }
    TensorField: *base-field
"));

            Random r = new Random(10);
            NetworkBuilder b = new NetworkBuilder();
            var m = new NamedBoxCollection();

            var maj = c.Major(r.NextDouble, m);
        }
    }
}
