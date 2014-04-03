using EpimetheusPlugins.Procedural;
using EpimetheusPlugins.Procedural.Utilities;

namespace Base_CityGeneration.Elements.Building.Facades
{
    /// <summary>
    /// Indicates that this node provides suggestions for what type of facade to place
    /// </summary>
    public interface IFacadeProvider
        : ISubdivisionContext
    {
        /// <summary>
        /// Suggest suitable facades for the given facade owner
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="section">The section of wall this facade will be for</param>
        /// <returns></returns>
        IFacade CreateFacade(IFacadeOwner owner, Walls.Section section);

        /// <summary>
        /// Configure the facade for the given facade owner
        /// </summary>
        /// <param name="facade"></param>
        /// <param name="owner"></param>
        void ConfigureFacade(IFacade facade, IFacadeOwner owner);
    }
}
