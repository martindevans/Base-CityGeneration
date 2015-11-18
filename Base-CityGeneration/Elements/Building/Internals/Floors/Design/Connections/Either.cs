using JetBrains.Annotations;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Design.Connections
{
    public class Either
        : BaseSpaceConnectionSpec
    {
        private readonly BaseSpaceConnectionSpec _a;
        public BaseSpaceConnectionSpec A { get { return _a; } }

        private readonly BaseSpaceConnectionSpec _b;
        public BaseSpaceConnectionSpec B { get { return _b; } }

        private readonly bool _exclusive;
        public bool Exclusive { get { return _exclusive; } }

        private Either(BaseSpaceConnectionSpec a, BaseSpaceConnectionSpec b, bool exclusive)
        {
            _a = a;
            _b = b;
            _exclusive = exclusive;
        }

        internal class Container
            : BaseContainer
        {
            public BaseContainer A { get; [UsedImplicitly]set; }
            public BaseContainer B { get; [UsedImplicitly]set; }
            public bool Exclusive { get; [UsedImplicitly]set; }

            public override BaseSpaceConnectionSpec Unwrap()
            {
                return new Either(
                    A.Unwrap(),
                    B.Unwrap(),
                    Exclusive
                );
            }
        }
    }
}
