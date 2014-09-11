using System;
using System.Collections.Generic;

namespace Base_CityGeneration.Elements.Building.Facades
{
    public interface IConfigurableFacade
        : IFacade
    {
        /// <summary>
        /// Get all the stamps placed on this facade so far
        /// </summary>
        IEnumerable<BaseFacade.Stamp> Stamps { get; }

        /// <summary>
        /// Add a stamp to this facade
        /// </summary>
        /// <param name="stamp"></param>
        /// <exception cref="InvalidOperationException">Thrown if this facade has already subdivided</exception>
        void AddStamp(BaseFacade.Stamp stamp);

        /// <summary>
        /// Do not place this entire facade
        /// </summary>
        void Delete();
    }
}
