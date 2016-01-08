using System;
using System.Collections.Generic;
using Base_CityGeneration.Elements.Building.Internals.Floors.Design.Spaces;
using Myre.Collections;
using SwizzleMyVectors.Geometry;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Design.SpaceMapping
{
    internal interface ISpaceMapper
    {
        /// <summary>
        /// Assign rectangular spaces to the given rooms
        /// </summary>
        /// <param name="region">The region to lay these spaces out in</param>
        /// <param name="spaces"></param>
        /// <param name="random"></param>
        /// <param name="metadata"></param>
        /// <returns></returns>
        IEnumerable<KeyValuePair<BoundingRectangle, BaseSpaceSpec>> Map(FloorplanRegion region, IEnumerable<KeyValuePair<BaseSpaceSpec, float>> spaces, Func<double> random, INamedDataCollection metadata);
    }
}
