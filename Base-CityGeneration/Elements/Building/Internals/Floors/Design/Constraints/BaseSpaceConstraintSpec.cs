using System;
using Myre.Collections;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Design.Constraints
{
    public abstract class BaseSpaceConstraintSpec
    {
        internal abstract class BaseContainer
            : IUnwrappable<BaseSpaceConstraintSpec>
        {
            public abstract BaseSpaceConstraintSpec Unwrap();
        }
    }
}
