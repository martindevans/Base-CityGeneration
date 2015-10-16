using System.Collections.Generic;
using EpimetheusPlugins.Procedural;

namespace Base_CityGeneration.Elements.Blocks
{
    /// <summary>
    /// Indicates that this node is surrounded by some neighbouring nodes
    /// </summary>
    public interface INeighbour
    {
        /// <summary>
        /// The nodes surrounding this node
        /// </summary>
        IReadOnlyList<ISubdivisionContext> Neighbours { get; set; }
    }
}
