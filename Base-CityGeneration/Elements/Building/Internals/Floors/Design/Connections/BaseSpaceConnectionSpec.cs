using System;
using Myre.Collections;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Design.Connections
{
    public abstract class BaseSpaceConnectionSpec
    {
        internal abstract class BaseContainer
            : IUnwrappable2<BaseSpaceConnectionSpec>
        {
            public abstract BaseSpaceConnectionSpec Unwrap(Func<double> random, INamedDataCollection metadata);
        }
    }
}
