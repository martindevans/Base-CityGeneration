using System.Collections.Generic;

namespace Base_CityGeneration.Elements.Building.Facades
{
    public interface IReadonlyConfigurableFacade
        : IFacade
    {
        /// <summary>
        /// Get all the stamps placed on this facade
        /// </summary>
        IEnumerable<BaseFacade.Stamp> Stamps { get; }
    }
}
