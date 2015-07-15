using Base_CityGeneration.Utilities.Numbers;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace Base_CityGeneration.Elements.Roads.Hyperstreamline.Tracing
{
    public class TracingConfiguration
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

        public BaseValueGenerator RoadWidth { get; set; }

        public TracingConfiguration()
        {
            MergeDistance = 25;
            SearchConeAngle = MathHelper.ToRadians(22.5f);
            SegmentLength = 10;

            RoadWidth = new UniformlyDistributedValue(1, 3);
        }

        #region serialization
        internal class Container
        {
            public List<string> Tags { get; set; }

            public List<object> Aliases { get; set; }

            public object MergeSearchAngle { get; set; }
            public object MergeDistance { get; set; }
            public object SegmentLength { get; set; }
            public object RoadWidth { get; set; }
            public Fields.Scalars.IScalarFieldContainer PriorityField { get; set; }
            public Fields.Scalars.IScalarFieldContainer SeparationField { get; set; }

            public Fields.Tensors.ITensorFieldContainer TensorField { get; set; }

            public TracingConfiguration Unwrap(Func<double> random)
            {
                return new TracingConfiguration {
                    SearchConeAngle = MathHelper.ToRadians(BaseValueGeneratorContainer.FromObject(MergeSearchAngle).SelectFloatValue(random)),
                    MergeDistance = BaseValueGeneratorContainer.FromObject(MergeDistance).SelectFloatValue(random),
                    SegmentLength = BaseValueGeneratorContainer.FromObject(SegmentLength).SelectFloatValue(random),
                    RoadWidth = BaseValueGeneratorContainer.FromObject(RoadWidth),
                    PriorityField = PriorityField.Unwrap(),
                    SeparationField = SeparationField.Unwrap(),
                    TensorField = TensorField.Unwrap(random)
                };
            }
        }
        #endregion
    }
}

