using EpimetheusPlugins.Scripts;
using Myre.Collections;
using System;
using System.Collections.Generic;

namespace Base_CityGeneration.Elements.Building.Design.Spec
{
    public interface IFloorSelector
    {
        IEnumerable<FloorSelection> Select(Func<double> random, INamedDataCollection metadata, Func<string[], ScriptReference> finder);
    }

    internal interface ISelectorContainer
    {
        IFloorSelector Unwrap();
    }
}
