using System.Linq;
using Base_CityGeneration.Elements.Roads.Hyperstreamline.Tracing;
using Base_CityGeneration.Utilities.Numbers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using System;
using System.IO;

namespace Base_CityGeneration.Test.Elements.Roads.Hyperstreamline
{
    [TestClass]
    public class NetworkBuilderTest
    {
        [TestMethod]
        public void NetworkBuilder_GeneratesMajorRoads()
        {
            var c = NetworkConfiguration.Deserialize(new StringReader(@"
!Network
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

            b.Build(c, new Random(10), new Vector2(0, 0), new Vector2(500, 500));
            b.Reduce();

            Console.WriteLine(b.Result.ToSvg());
        }

        [TestMethod]
        public void NetworkBuilder_GeneratesRegions()
        {
            var c = NetworkConfiguration.Deserialize(new StringReader(@"
!Network
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

            b.Build(c, new Random(10), new Vector2(0, 0), new Vector2(500, 500));
            b.Reduce();

            var regions = b.Regions();

            Console.WriteLine(b.Result.ToSvg(regions));
        }

        [TestMethod]
        public void NetworkBuilder_GeneratesMinorRoads()
        {
            var c = NetworkConfiguration.Deserialize(new StringReader(@"
!Network
Aliases:
    - &base-field !AddTensors
      Tensors:
        - !PointDistanceDecayTensors { Tensors: !Grid { Angle: 0, Length: 1 }, Decay: 2.5, Center: { X: 0, Y: 0 } }
        - !PointDistanceDecayTensors { Tensors: !Grid { Angle: 45, Length: 1 }, Decay: 2.5, Center: { X: 1, Y: 0 } }
        - !PointDistanceDecayTensors { Tensors: !Grid { Angle: 45, Length: 1 }, Decay: 2.5, Center: { X: 0, Y: 1 } }
        - !PointDistanceDecayTensors { Tensors: !Grid { Angle: 0, Length: 1 }, Decay: 2.5, Center: { X: 1, Y: 1 } }
MergeSearchAngle: 22.5
MergeDistance: 25
SegmentLength: 10
RoadWidth: !NormalValue { Min: 2, Max: 4, Vary: true }
PriorityField: !ConstantScalars { Value: 1 }
SeparationField: !ConstantScalars { Value: 50 }
TensorField: *base-field
"));

            NetworkBuilder b = new NetworkBuilder();

            b.Build(c, new Random(10), new Vector2(0, 0), new Vector2(500, 500));
            b.Reduce();

            var regions = b.Regions();

            c.RoadWidth = new UniformlyDistributedValue(1, 3, true);
            b.Build(c, new Random(20), regions.Skip(3).First());

            Console.WriteLine(b.Result.ToSvg());
        }
    }
}
