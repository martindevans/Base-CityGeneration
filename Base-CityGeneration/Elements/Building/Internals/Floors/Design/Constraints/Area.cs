using System;
using System.Diagnostics.Contracts;
using Base_CityGeneration.Utilities.Numbers;
using JetBrains.Annotations;
using Myre.Collections;
using MathHelper = Microsoft.Xna.Framework.MathHelper;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Design.Constraints
{
    /// <summary>
    /// Indicates that this space must have a certain area
    /// </summary>
    public class Area
        : BaseSpaceConstraintSpec
    {
        private readonly float _minimum;
        public float Minimum { get { return _minimum; } }

        private readonly float _maximum;
        public float Maximum { get { return _maximum; } }

        private Area(float min, float max)
        {
            _minimum = min;
            _maximum = max;
        }

        public override float AssessSatisfactionProbability(FloorplanRegion region)
        {
            //Calculate how much space we need vs how much there is available
            var required = Minimum;
            var available = region.UnassignedArea;

            //If insufficient area is available insta-fail
            if (available < required)
                return 0;

            //Increase chance as more area becomes available, chance becomes 100% (and maxes out) when available/required ratio is e/2 (2.78/2)
            //Multiply by 1.5 so the lowest chance is Log(1*1.5)==0.405
            return MathHelper.Clamp((float)Math.Log((available / required) * 1.5f), 0.1f, 1);
        }

        public override bool IsSatisfied(FloorplanRegion region)
        {
            return region.Area >= _minimum && region.Area <= _maximum;
        }

        internal override T Union<T>(T other)
        {
            return Union(other as Area) as T;
        }

        private Area Union(Area area)
        {
            Contract.Requires(area != null);

            return new Area(_minimum + area.Minimum, _maximum + area.Maximum);
        }

        internal class Container
            : BaseContainer
        {
            public object Min { get; [UsedImplicitly]set; }
            public object Max { get; [UsedImplicitly]set; }

            public override BaseSpaceConstraintSpec Unwrap(Func<double> random, INamedDataCollection metadata)
            {
                return new Area(
                    IValueGeneratorContainer.FromObject(Min, 1).SelectFloatValue(random, metadata),
                    IValueGeneratorContainer.FromObject(Max, float.PositiveInfinity).SelectFloatValue(random, metadata)
                );
            }
        }
    }
}
