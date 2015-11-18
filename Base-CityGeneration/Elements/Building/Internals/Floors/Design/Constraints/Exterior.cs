using Base_CityGeneration.Utilities.Numbers;

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

        private Area(IValueGenerator min = null, IValueGenerator max = null)
        {
            Minimum = min ?? new ConstantValue(1);
            Maximum = max ?? new ConstantValue(float.PositiveInfinity);
        }

        internal class Container
            : BaseContainer
        {
            public object Min { get; set; }
            public object Max { get; set; }

            public override BaseSpaceConstraintSpec Unwrap()
            {
                return new Area(
                    BaseValueGeneratorContainer.FromObject(Min),
                    BaseValueGeneratorContainer.FromObject(Max)
                );
            }
        }
    }
}
