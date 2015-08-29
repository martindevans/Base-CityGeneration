using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Base_CityGeneration.Datastructures.HalfEdge;
using Base_CityGeneration.Elements.Roads;
using Base_CityGeneration.Elements.Roads.Hyperstreamline;
using Base_CityGeneration.Elements.Roads.Hyperstreamline.Tracing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Numerics;
using Myre.Collections;

namespace Base_CityGeneration.Test.Elements.Roads.Hyperstreamline
{
    [TestClass]
    public class ConvertToMesh
    {
        private const string SCRIPT = @"!Network
Aliases:
    - &base-field !AddTensors
      Tensors:
        - !PointDistanceDecayTensors { Tensors: !Radial { Center: { X: 0.5, Y: 0.5 } }, Decay: 12, Center: { X: 0.5, Y: 0.5 } }
        - !PointDistanceDecayTensors { Tensors: !Grid { Angle: 45 }, Decay: 7, Center: { X: 0, Y: 0 } }
        - !PointDistanceDecayTensors { Tensors: !Grid { Angle: 60 }, Decay: 7, Center: { X: 1, Y: 0 } }
        - !PointDistanceDecayTensors { Tensors: !Grid { Angle: 36 }, Decay: 7, Center: { X: 0, Y: 1 } }
        - !PointDistanceDecayTensors { Tensors: !Grid { Angle: 90 }, Decay: 7, Center: { X: 1, Y: 1 } }
        - !Polyline
          Decay: 30
          Points:
            - { X: 0.15, Y: 0 }
            - { X: 0.07, Y: 0.25 }
            - { X: 0.1, Y: 0.45 }
            - { X: 0.3, Y: 1 }

Major:
    MergeSearchAngle: 22.5
    MergeDistance: 25
    SegmentLength: 10
    RoadWidth: !NormalValue { Min: 2, Max: 4, Vary: true }
    PriorityField: !ConstantScalars { Value: 1 }
    SeparationField: !ConstantScalars { Value: 100 }
    TensorField: *base-field

Minor:
    MergeSearchAngle: 12.5
    MergeDistance: 2.5
    SegmentLength: 2
    RoadWidth: !NormalValue { Min: 1, Max: 2, Vary: true }
    PriorityField: !ConstantScalars { Value: 1 }
    SeparationField: !ConstantScalars { Value: 15 }
    TensorField:
        !WeightedAverage
        Tensors:
            1: *base-field
            0.2: !Grid { Angle: !UniformValue { Min: 1, Max: 360, Vary: true } }
";

        [TestMethod]
        public void ConvertFromHyperstreamlineToHalfEdgeMesh()
        {
            var r = new Random();
            Func<double> random = r.NextDouble;
            var m = new NamedBoxCollection();

            //Deserialize config
            var config = NetworkDescriptor.Deserialize(new StringReader(SCRIPT));

            //Build main roads
            var builder = new NetworkBuilder();
            builder.Build(config.Major(random, m), random, m, new Vector2(0, 0), new Vector2(100, 100));
            builder.Reduce();

            //extract regions
            var regions = builder.Regions();

            //Build minor roads
            foreach (var region in regions)
                builder.Build(config.Minor(random, m), random, m, region);
            builder.Reduce();

            //Build graph
            Mesh<IVertexBuilder, IHalfEdgeBuilder, IFaceBuilder> mesh = new Mesh<IVertexBuilder, IHalfEdgeBuilder, IFaceBuilder>();

            //Extract vertices
            var result = builder.Result;
            Dictionary<Vector2, Vertex> vertices = result.Vertices.ToDictionary(v => v.Position, v => v);

            //Useful data
            var road = 3;//HierarchicalParameters.RoadLaneWidth(Random);
            var path = 1;//HierarchicalParameters.RoadSidewalkWidth(Random);

            //Create blocks
            var blocks = builder.Regions();
            foreach (var block in blocks)
            {
                var face = mesh.GetOrConstructFace(block.Vertices.Select(mesh.GetOrConstructVertex).ToArray());

                //Tags edges of face with road width
                foreach (var primaryEdge in face.Edges.Select(e => e.IsPrimaryEdge ? e : e.Pair))
                {
                    if (primaryEdge.Tag != null)
                        continue;

                    var start = vertices[primaryEdge.Pair.EndVertex.Position];
                    var end = vertices[primaryEdge.EndVertex.Position];
                    var edge = start.Edges.FirstOrDefault(a => Equals(a.B, end));

                    primaryEdge.Tag = new HalfEdgeRoadBuilder(primaryEdge, road, path, edge == null ? 1 : edge.Streamline.Width);
                }
            }
        }
    }
}
