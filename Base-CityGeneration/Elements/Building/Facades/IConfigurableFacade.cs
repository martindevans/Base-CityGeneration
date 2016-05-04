using System;
using EpimetheusPlugins.Procedural;

namespace Base_CityGeneration.Elements.Building.Facades
{
    public interface IConfigurableFacade
        : IReadonlyConfigurableFacade
    {
        /// <summary>
        /// Add a stamp to this facade
        /// </summary>
        /// <param name="stamp"></param>
        /// <exception cref="InvalidOperationException">Thrown if this facade has already subdivided</exception>
        void AddStamp(BaseFacade.Stamp stamp);

        /// <summary>
        /// Get a context which can be depended upon (for ordering of subdivision)
        /// </summary>
        /// <returns></returns>
        ISubdivisionContext GetDependencyContext();
    }
}
