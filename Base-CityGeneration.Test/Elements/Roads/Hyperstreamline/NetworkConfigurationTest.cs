using Base_CityGeneration.Elements.Roads.Hyperstreamline;
using Base_CityGeneration.Elements.Roads.Hyperstreamline.Fields.Scalars;
using Base_CityGeneration.Utilities.Numbers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Numerics;
using System.IO;

using MathHelper = Microsoft.Xna.Framework.MathHelper;

namespace Base_CityGeneration.Test.Elements.Roads.Hyperstreamline
{
    [TestClass]
    public class NetworkConfigurationTest
    {
        [TestMethod]
        public void NetworkConfiguration_CanDeserialize()
        {
            var d = NetworkDescriptor.Deserialize(new StringReader(@"
!Network
Major:
    MergeSearchAngle: 22.5
    MergeDistance: 25
    SegmentLength: 10
    RoadWidth: !UniformValue { Min: 1, Max: 3 }
    PriorityField: !ConstantScalars { Value: 1 }
    SeparationField: !ConstantScalars { Value: 50 }
    TensorField: !AddTensors
        Tensors:
            - !PointDistanceDecayTensors { Tensors: !Radial { Center: { X: 0.45, Y: 0.45 } }, Decay: 0.00008, Center: { X: 0.45, Y: 0.45 } }
            - !PointDistanceDecayTensors { Tensors: !Grid { Angle: 10, Length: 1 }, Decay: 0.000025, Center: { X: 0, Y: 0 } }
            - !PointDistanceDecayTensors { Tensors: !Grid { Angle: 20, Length: 1 }, Decay: 0.000025, Center: { X: 1, Y: 0 } }
            - !PointDistanceDecayTensors { Tensors: !Grid { Angle: 30, Length: 1 }, Decay: 0.000025, Center: { X: 0, Y: 1 } }
            - !PointDistanceDecayTensors { Tensors: !Grid { Angle: 40, Length: 1 }, Decay: 0.000025, Center: { X: 1, Y: 1 } }
            - !Polyline
                Decay: 0.000025
                Points:
                    - { X: 0.15, Y: 0 }
                    - { X: 0.07, Y: 0.25 }
                    - { X: 0.1, Y: 0.45 }
                    - { X: 0.3, Y: 1 }
"));

            var c = d.Major(() => 1, null);

            Assert.AreEqual(22.5f, MathHelper.ToDegrees(c.SearchConeAngle), 0.001f);
            Assert.AreEqual(25f, c.MergeDistance);
            Assert.AreEqual(10f, c.SegmentLength);
            Assert.IsInstanceOfType(c.RoadWidth, typeof(UniformlyDistributedValue));
            Assert.IsInstanceOfType(c.PriorityField, typeof(Constant));
            Assert.IsInstanceOfType(c.SeparationField, typeof(Constant));
        }

        [TestMethod]
        public void NetworkConfiguration_CanDeserialize_T2()
        {
            var d = NetworkDescriptor.Deserialize(new StringReader(@"
!Network
Major:
    MergeSearchAngle: 22.5
    MergeDistance: 25
    SegmentLength: 10
    RoadWidth: !UniformValue { Min: 1, Max: 3 }
    PriorityField: !ConstantScalars { Value: 1 }
    SeparationField: !ConstantScalars { Value: 50 }
    TensorField: !AddTensors
        Tensors:
            - !PointDistanceDecayTensors { Tensors: !Radial { Center: { X: 0.45, Y: 0.45 } }, Decay: 0.00008, Center: { X: 0.45, Y: 0.45 } }
            - !PointDistanceDecayTensors { Tensors: !Grid { Angle: 10, Length: 1 }, Decay: 0.000025, Center: { X: 0, Y: 0 } }
            - !PointDistanceDecayTensors { Tensors: !Grid { Angle: 20, Length: 1 }, Decay: 0.000025, Center: { X: 1, Y: 0 } }
            - !PointDistanceDecayTensors { Tensors: !Grid { Angle: 30, Length: 1 }, Decay: 0.000025, Center: { X: 0, Y: 1 } }
            - !PointDistanceDecayTensors { Tensors: !Grid { Angle: 40, Length: 1 }, Decay: 0.000025, Center: { X: 1, Y: 1 } }
            - !Polyline
                Decay: 0.000025
                Points:
                    - { X: 0.15, Y: 0 }
                    - { X: 0.07, Y: 0.25 }
                    - { X: 0.1, Y: 0.45 }
                    - { X: 0.3, Y: 1 }
"));
            var c = d.Major(() => 1, null);

            Assert.AreEqual(22.5f, MathHelper.ToDegrees(c.SearchConeAngle), 0.001f);
            Assert.AreEqual(25f, c.MergeDistance);
            Assert.AreEqual(10f, c.SegmentLength);
            Assert.IsInstanceOfType(c.RoadWidth, typeof(UniformlyDistributedValue));
            Assert.IsInstanceOfType(c.PriorityField, typeof(Constant));
            Assert.IsInstanceOfType(c.SeparationField, typeof(Constant));
        }
    }
}
