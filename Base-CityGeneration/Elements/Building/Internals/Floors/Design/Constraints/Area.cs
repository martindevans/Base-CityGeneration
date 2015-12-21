using System;
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
        public IValueGenerator Minimum { get; private set; }
        public IValueGenerator Maximum { get; private set; }

        private Area(IValueGenerator min, IValueGenerator max)
        {
            Minimum = min.Transform(vary: false);
            Maximum = max.Transform(vary: false);
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
            return new Area(Minimum.Add(area.Minimum), Maximum.Add(area.Maximum));
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
