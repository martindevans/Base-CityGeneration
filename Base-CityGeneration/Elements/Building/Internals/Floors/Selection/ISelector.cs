
using System;
using System.Collections.Generic;
using EpimetheusPlugins.Scripts;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Selection
{
    public interface ISelector
    {
        IEnumerable<ScriptReference> Select(Func<double> random, ScriptReference[] verticals, Func<string[], ScriptReference> finder);
    }

    internal interface ISelectorContainer
    {
        ISelector Unwrap();
    }
}
