using System.Collections.Generic;
using System.Linq;
using Base_CityGeneration.Elements.Generic;
using Base_CityGeneration.Parcels.Parcelling;
using EpimetheusPlugins.Procedural;
using System.Numerics;
using Myre.Collections;

namespace Base_CityGeneration.Elements.Blocks
{
    public abstract class BaseBlock
        :ProceduralScript, IGrounded
    {
        public float GroundHeight { get; set; }

        public override void Subdivide(Prism bounds, ISubdivisionGeometry geometry, INamedDataCollection hierarchicalParameters)
        {
            //Create land parcels
            var nodes = CreateParcelNodes(GenerateParcels(bounds.Footprint).ToArray(), bounds.Height);

            foreach (var grounded in nodes.Select(node => node.Value).OfType<IGrounded>())
                grounded.GroundHeight = GroundHeight;
        }

        /// <summary>
        /// Given the footprint of the entire block, generate parcels for buildings in the block
        /// </summary>
        /// <param name="footprint"></param>
        /// <returns></returns>
        protected abstract IEnumerable<Parcel> GenerateParcels(IEnumerable<Vector2> footprint);

        /// <summary>
        /// Create nodes filling the given parcels
        /// </summary>
        /// <param name="parcels"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        protected abstract IEnumerable<KeyValuePair<Parcel, ISubdivisionContext>> CreateParcelNodes(Parcel[] parcels, float height);
    }
}
