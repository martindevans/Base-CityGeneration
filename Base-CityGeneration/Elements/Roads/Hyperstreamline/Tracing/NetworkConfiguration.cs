using System;
using System.Collections.Generic;
using System.IO;
using Base_CityGeneration.Utilities.Numbers;
using Microsoft.Xna.Framework;
using SharpYaml.Serialization;

namespace Base_CityGeneration.Elements.Roads.Hyperstreamline.Tracing
{
    public class NetworkConfiguration
    {
        /// <summary>
        /// Field which specifies the priority of a given point as a seed. Recommended that this contains normalized distance from nearest coastline
        /// </summary>
        public Fields.Scalars.BaseScalarField PriorityField { get; set; }

        /// <summary>
        /// Separation between roads
        /// </summary>
        public Fields.Scalars.BaseScalarField SeparationField { get; set; }

        /// <summary>
        /// Eigen vectors to trace streamlines along
        /// </summary>
        public Fields.Tensors.ITensorField TensorField { get; set; }

        internal float CosineSearchConeAngle { get; private set; }
        public float SearchConeAngle
        {
            get
            {
                return (float) Math.Acos(CosineSearchConeAngle);
            }
            set
            {
                CosineSearchConeAngle = (float) Math.Cos(value);
            }
        }

        internal float SegmentLengthSquared { get; private set; }
        private float _segmentLength;
        /// <summary>
        /// The approximate length of a segment of road
        /// </summary>
        public float SegmentLength
        {
            get
            {
                return _segmentLength;
            }
            set
            {
                _segmentLength = value;
                SegmentLengthSquared = value * value;
            }
        }

        public float MergeDistance { get; set; }

        public IValueGenerator RoadWidth { get; set; }

        public NetworkConfiguration()
        {
            MergeDistance = 25;
            SearchConeAngle = MathHelper.ToRadians(22.5f);
            SegmentLength = 10;

            RoadWidth = new UniformlyDistributedValue(1, 3);
        }

        #region serialization
        private static Serializer CreateSerializer()
        {
            var serializer = new Serializer(new SerializerSettings
            {
                EmitTags = true,
            });

            //Fields Types
            serializer.Settings.RegisterTagMapping("ConstantScalars", typeof(Fields.Scalars.Constant.Container));
            //serializer.Settings.RegisterTagMapping("ConstantTensors", typeof(Fields.Tensors.Constant.Container));
            //serializer.Settings.RegisterTagMapping("ConstantVectors", typeof(Fields.Vectors.Constant.Container));
            serializer.Settings.RegisterTagMapping("AddTensors", typeof(Fields.Tensors.Addition.Container));
            serializer.Settings.RegisterTagMapping("PointDistanceDecayTensors", typeof(Fields.Tensors.PointDistanceDecayField.Container));
            serializer.Settings.RegisterTagMapping("Radial", typeof(Fields.Tensors.Radial.Container));
            serializer.Settings.RegisterTagMapping("Grid", typeof(Fields.Tensors.Gridline.Container));
            serializer.Settings.RegisterTagMapping("Polyline", typeof(Fields.Tensors.Polyline.Container));

            //Network Types
            serializer.Settings.RegisterTagMapping("Network", typeof(Container));

            //Utility types
            serializer.Settings.RegisterTagMapping("NormalValue", typeof(NormallyDistributedValue.Container));
            serializer.Settings.RegisterTagMapping("UniformValue", typeof(UniformlyDistributedValue.Container));
            serializer.Settings.RegisterTagMapping("ConstantValue", typeof(UniformlyDistributedValue.Container));

            return serializer;
        }

        public static NetworkConfiguration Deserialize(TextReader reader)
        {
            var s = CreateSerializer();

            return s.Deserialize<Container>(reader).Unwrap();
        }

        internal class Container
        {
            public List<string> Tags { get; set; }

            public List<object> Aliases { get; set; }

            public float MergeSearchAngle { get; set; }
            public float MergeDistance { get; set; }
            public float SegmentLength { get; set; }
            public BaseValueGeneratorContainer RoadWidth { get; set; }
            public Fields.Scalars.IScalarFieldContainer PriorityField { get; set; }
            public Fields.Scalars.IScalarFieldContainer SeparationField { get; set; }

            public Fields.Tensors.ITensorFieldContainer TensorField { get; set; }

            public NetworkConfiguration Unwrap()
            {
                return new NetworkConfiguration {
                    SearchConeAngle = MathHelper.ToRadians(MergeSearchAngle),
                    MergeDistance = MergeDistance,
                    SegmentLength = SegmentLength,
                    RoadWidth = RoadWidth.Unwrap(),
                    PriorityField = PriorityField.Unwrap(),
                    SeparationField = SeparationField.Unwrap(),
                    TensorField = TensorField.Unwrap()
                };
            }
        }
        #endregion
    }
}

