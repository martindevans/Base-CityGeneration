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
        private readonly IValueGenerator _minimum;
        public IValueGenerator Minimum { get { return _minimum; } }

        private readonly IValueGenerator _maximum;
        public IValueGenerator Maximum { get { return _maximum; } }

        private Area(IValueGenerator min, IValueGenerator max)
        {
            Contract.Requires(min != null, "min");
            Contract.Requires(max != null, "max");

            _minimum = min.Transform(vary: false);
            _maximum = max.Transform(vary: false);
        }

        public override float AssessSatisfactionProbability(FloorplanRegion region, Func<double> random, INamedDataCollection metadata)
        {
            //Calculate how much space we need vs how much there is available
            var required = Minimum.SelectFloatValue(random, metadata);
            var available = region.UnassignedArea;

            //If insufficient area is available insta-fail
            if (available < required)
                return 0;

            //Increase chance as more area becomes available, chance becomes 100% (and maxes out) when available/required ratio is e/2 (2.78/2)
            //Multiply by 1.5 so the lowest chance is Log(1*1.5)==0.405
            return MathHelper.Clamp((float)Math.Log((available / required) * 1.5f), 0.1f, 1);
        }

        internal override T Union<T>(T other)
        {
            return Union(other as Area) as T;
        }

        private Area Union(Area area)
        {
            Contract.Requires(area != null, "area");

            return new Area(_minimum.Add(area.Minimum), _maximum.Add(area.Maximum));
        }

        internal class Container
            : BaseContainer
        {
            public object Min { get; [UsedImplicitly]set; }
            public object Max { get; [UsedImplicitly]set; }

            public override BaseSpaceConstraintSpec Unwrap()
            {
                return new Area(
                    BaseValueGeneratorContainer.FromObject(Min, 1),
                    BaseValueGeneratorContainer.FromObject(Max, float.PositiveInfinity)
                );
            }
        }
    }
}
