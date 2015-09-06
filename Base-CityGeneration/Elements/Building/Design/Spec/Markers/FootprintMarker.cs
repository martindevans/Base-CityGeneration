using EpimetheusPlugins.Scripts;
using Myre.Collections;
using System;
using System.Collections.Generic;

namespace Base_CityGeneration.Elements.Building.Design.Spec.Markers
{
    public class FootprintMarker
        : BaseMarker
    {
        public override IEnumerable<FloorSelection> Select(Func<double> random, INamedDataCollection metadata, Func<string[], ScriptReference> finder)
        {
            yield break;
        }

        internal class Container
            : ISelectorContainer
        {
            public BaseFloorSelector Unwrap()
            {
                return new FootprintMarker();
            }
        }
    }
}
