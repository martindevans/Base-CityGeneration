using System;
using System.Collections.Generic;
using System.Linq;
using EpimetheusPlugins.Procedural;
using EpimetheusPlugins.Procedural.Utilities;

namespace Base_CityGeneration.Elements.Building.Facades
{
    /// <summary>
    /// Indicates that this element will place a facade
    /// </summary>
    public interface IFacadeOwner
        : ISubdivisionContext
    {
    }

    /// <summary>
    /// Decorator methods for IFacadeOwner
    /// </summary>
    public static class IFacadeOwnerExtensions
    {
        /// <summary>
        /// Searches up the tree of parent nodes to find suggestions for the facade of this element
        /// </summary>
        /// <param name="start">The node to start searching from</param>
        /// <param name="section">The section of wall this facade will fill</param>
        /// <param name="endStop">A set of types which the search will stop at if encountered. nodes with these types will be the last asked</param>
        /// <returns></returns>
        public static IFacade FindFacade(this IFacadeOwner start, Walls.Section section, params Type[] endStop)
        {
            ISubdivisionContext node = start.Parent;

            while (node != null)
            {
                //Once we find a provider take suggestions from it
                var p = node as IFacadeProvider;
                if (p != null)
                {
                    var f = p.CreateFacade(start, section);
                    if (f != null)
                        p.ConfigureFacade(f, start);
                    return f;
                }

                if (endStop.Any(t => t.IsInstanceOfType(node)))
                    break;

                //Search further up
                node = node.Parent;
            }

            //Found nothing
            return null;
        }
    }
}
