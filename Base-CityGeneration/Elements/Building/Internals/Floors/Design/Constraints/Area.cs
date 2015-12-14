using Base_CityGeneration.Utilities.Numbers;
using JetBrains.Annotations;

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
