using System;
using System.Collections.Generic;
using Myre.Collections;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Design.Spaces
{
    /// <summary>
    /// Produces space specs to include in a floor plan
    /// </summary>
    public interface ISpaceSpecProducer
    {
        /// <summary>
        /// Produce specs
        /// </summary>
        /// <param name="required">If true, return specs which *must* be fit into the floorplan. If false, return specs to *try* and fit into the floorplan</param>
        /// <param name="random"></param>
        /// <param name="metadata"></param>
        /// <returns></returns>
        IEnumerable<BaseSpaceSpec> Produce(bool required, Func<double> random, INamedDataCollection metadata); 
    }

    internal interface ISpaceSpecProducerContainer
        : IUnwrappable<ISpaceSpecProducer>
    {
    }
}
