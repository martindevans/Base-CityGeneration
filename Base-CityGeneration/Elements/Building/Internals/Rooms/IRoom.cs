using Base_CityGeneration.Elements.Building.Facades;
using Base_CityGeneration.Parcelling;
using EpimetheusPlugins.Procedural;

namespace Base_CityGeneration.Elements.Building.Internals.Rooms
{
    public interface IRoom
        : ISubdivisionContext, IParcelElement<IRoom>, IFacadeOwner
    {
    }
}
