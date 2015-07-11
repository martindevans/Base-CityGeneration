using System;
using Base_CityGeneration.Elements.Roads.Hyperstreamline.Fields.Scalars;

namespace Base_CityGeneration.Elements.Roads.Hyperstreamline.Tracing
{
    public class NetworkConfiguration
    {
        /// <summary>
        /// Rotation field which perturbs major and minor eigen vectors in equal and opposite amounts
        /// </summary>
        public BaseScalarField RotationField { get; set; }

        /// <summary>
        /// Rotation field which only perturbs major eigen vectors
        /// </summary>
        public BaseScalarField MajorRotationField { get; set; }

        /// <summary>
        /// Rotation field which only perturbs minor eigen vectors
        /// </summary>
        public BaseScalarField MinorRotationField { get; set; }

        /// <summary>
        /// Field which specifies the priority of a given point as a seed. Recommended that this contains normalized distance from nearest coastline
        /// </summary>
        public BaseScalarField PriorityField { get; set; }

        /// <summary>
        /// Separation between roads
        /// </summary>
        public BaseScalarField SeparationField { get; set; }

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

        public int MajorRoadMinWidth { get; set; }
        public int MajorRoadMaxWidth { get; set; }

        public int MinorRoadMinWidth { get; set; }
        public int MinorRoadMaxWidth { get; set; }

        public NetworkConfiguration(float mergeDistance, float searchConeAngle, float segmentLength)
        {
            MergeDistance = mergeDistance;
            SearchConeAngle = searchConeAngle;
            SegmentLength = segmentLength;

            MajorRoadMaxWidth = 6;
            MajorRoadMinWidth = 4;

            MinorRoadMaxWidth = 2;
            MinorRoadMinWidth = 2;
        }
    }
}
