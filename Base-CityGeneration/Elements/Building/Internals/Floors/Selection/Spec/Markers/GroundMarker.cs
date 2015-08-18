﻿using System;
using System.Collections.Generic;
using EpimetheusPlugins.Scripts;
using Myre.Collections;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Selection.Spec.Markers
{
    /// <summary>
    /// Marks where the ground is in a sequence
    /// </summary>
    public class GroundMarker
        : IMarker
    {
        public IEnumerable<FloorSelection> Select(Func<double> random, INamedDataCollection metadata, Func<string[], ScriptReference> finder)
        {
            yield break;
        }

        internal class Container
            :ISelectorContainer
        {
            public ISelector Unwrap()
            {
                return new GroundMarker();
            }
        }
    }
}
