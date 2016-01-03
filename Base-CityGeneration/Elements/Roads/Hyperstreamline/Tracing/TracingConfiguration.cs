using Base_CityGeneration.Utilities.Numbers;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Base_CityGeneration.Elements.Roads.Hyperstreamline.Fields.Scalars;
using Base_CityGeneration.Elements.Roads.Hyperstreamline.Fields.Tensors;
using Myre.Collections;

using MathHelper = Microsoft.Xna.Framework.MathHelper;

namespace Base_CityGeneration.Elements.Roads.Hyperstreamline.Tracing
{
    public class TracingConfiguration
    {
        private readonly BaseScalarField _priorityField;
        /// <summary>
        /// Field which specifies the priority of a given point as a seed. Recommended that this contains normalized distance from nearest coastline
        /// </summary>
        public BaseScalarField PriorityField
        {
            get
            {
                Contract.Ensures(Contract.Result<BaseScalarField>() != null);
                return _priorityField;
            }
        }

        private readonly BaseScalarField _separationField;
        /// <summary>
        /// Separation between roads
        /// </summary>
        public BaseScalarField SeparationField
        {
            get
            {
                Contract.Ensures(Contract.Result<BaseScalarField>() != null);
                return _separationField;
            }
        }

        private readonly ITensorField _tensorField;
        /// <summary>
        /// Eigen vectors to trace streamlines along
        /// </summary>
        public ITensorField TensorField
        {
            get
            {
                Contract.Ensures(Contract.Result<ITensorField>() != null);
                return _tensorField;
            }
        }

        private readonly float _consineSearchConeAngle;
        internal float CosineSearchConeAngle
        {
            get { return _consineSearchConeAngle; }
        }

        public float SearchConeAngle
        {
            get
            {
                return (float)Math.Acos(_consineSearchConeAngle);
            }
        }

        private readonly float _segmentLength;
        /// <summary>
        /// The approximate length of a segment of road
        /// </summary>
        public float SegmentLength
        {
            get
            {
                return _segmentLength;
            }
        }

        private readonly float _mergeDistance;
        public float MergeDistance
        {
            get { return _mergeDistance; }
        }

        private readonly IValueGenerator _roadWidth;
        public IValueGenerator RoadWidth
        {
            get
            {
                Contract.Ensures(Contract.Result<IValueGenerator>() != null);
                return _roadWidth;
            }
        }

        public TracingConfiguration(BaseScalarField priorityField, BaseScalarField separationField, ITensorField tensorField, IValueGenerator roadWidth,
            float searchAngle = 0.3926991f, //22.5 degrees in radians
            float segmentLength = 10,
            float mergeDistance = 25
        )
        {
            Contract.Requires(priorityField != null);
            Contract.Requires(separationField != null);
            Contract.Requires(tensorField != null);
            Contract.Requires(roadWidth != null);

            _priorityField = priorityField;
            _separationField = separationField;
            _tensorField = tensorField;
            _roadWidth = roadWidth;
            _consineSearchConeAngle = (float)Math.Cos(searchAngle);
            _segmentLength = segmentLength;
            _mergeDistance = mergeDistance;
        }

        [ContractInvariantMethod]
        private void ObjectInvariants()
        {
            Contract.Invariant(_priorityField != null);
            Contract.Invariant(_separationField != null);
            Contract.Invariant(_tensorField != null);
            Contract.Invariant(_roadWidth != null);
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
            public IScalarFieldContainer PriorityField { get; set; }
            public IScalarFieldContainer SeparationField { get; set; }

            public ITensorFieldContainer TensorField { get; set; }

            public TracingConfiguration Unwrap(Func<double> random, INamedDataCollection metadata)
            {
                return new TracingConfiguration(
                    PriorityField.Unwrap(),
                    SeparationField.Unwrap(),
                    TensorField.Unwrap(random, metadata),
                    BaseValueGeneratorContainer.FromObject(RoadWidth),
                    MathHelper.ToRadians(BaseValueGeneratorContainer.FromObject(MergeSearchAngle).SelectFloatValue(random, metadata)),
                    BaseValueGeneratorContainer.FromObject(SegmentLength).SelectFloatValue(random, metadata),
                    BaseValueGeneratorContainer.FromObject(MergeDistance).SelectFloatValue(random, metadata));
            }
        }
        #endregion
    }
}

