using System;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Design.Spaces.Constraints
{
    public abstract class BaseConstraint
    {

        internal abstract class BaseContainer
            : IUnwrappable<BaseConstraint>
        {
            public BaseConstraint Unwrap()
            {
                throw new NotImplementedException();
            }
        }
    }
}
