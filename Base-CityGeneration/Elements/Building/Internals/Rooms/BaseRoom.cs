﻿using EpimetheusPlugins.Procedural;
using EpimetheusPlugins.Scripts;
using Myre.Collections;

namespace Base_CityGeneration.Elements.Building.Internals.Rooms
{
    [Script("E655C852-8B0E-460B-BD30-35158DA1053C", "Base Room")]
    public class BaseRoom
        : ProceduralScript, IRoom
    {
        public override bool Accept(Prism bounds, INamedDataProvider parameters)
        {
            return true;
        }

        public override void Subdivide(Prism bounds, ISubdivisionGeometry geometry, INamedDataCollection hierarchicalParameters)
        {
        }
    }
}
