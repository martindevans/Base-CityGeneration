namespace Base_CityGeneration.Elements.Building.Internals.Floors.Design.Connections
{
    public abstract class BaseSpaceConnectionSpec
    {
        internal abstract class BaseContainer
            : IUnwrappable<BaseSpaceConnectionSpec>
        {
            public abstract BaseSpaceConnectionSpec Unwrap();
        }
    }
}
