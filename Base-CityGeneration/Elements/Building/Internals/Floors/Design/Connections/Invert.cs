using JetBrains.Annotations;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Design.Connections
{
    public class Invert
        : BaseSpaceConnectionSpec
    {
        private readonly BaseSpaceConnectionSpec _condition;
        public BaseSpaceConnectionSpec Condition { get { return _condition; }
        }

        public Invert(BaseSpaceConnectionSpec condition)
        {
            _condition = condition;
        }

        internal class Container
            : BaseContainer
        {
            public BaseContainer Inner { get; [UsedImplicitly]set; }

            public override BaseSpaceConnectionSpec Unwrap()
            {
                return new Invert(Inner.Unwrap());
            }
        }
    }
}
