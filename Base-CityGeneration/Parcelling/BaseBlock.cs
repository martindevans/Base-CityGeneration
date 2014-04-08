using System.Collections.Generic;
using System.Linq;
using Base_CityGeneration.Elements.Generic;
using EpimetheusPlugins.Procedural;
using Microsoft.Xna.Framework;
using Myre.Collections;

namespace Base_CityGeneration.Parcelling
{
    public abstract class BaseBlock
        :ProceduralScript, IGrounded
    {
        public float GroundHeight { get; set; }

        public override void Subdivide(Prism bounds, ISubdivisionGeometry geometry, INamedDataCollection hierarchicalParameters)
        {
            var nodes = CreateParcelNodes(GenerateParcels(bounds.Footprint).ToArray(), bounds.Height);
            foreach (var node in nodes)
            {
                var grounded = node.Value as IGrounded;
                if (grounded != null)
                    grounded.GroundHeight = GroundHeight;
            }
        }

        /// <summary>
        /// Given the footprint of the entire block, generate parcels for buildings in the block
        /// </summary>
        /// <param name="footprint"></param>
        /// <returns></returns>
        protected abstract IEnumerable<Parcel> GenerateParcels(Vector2[] footprint);

        /// <summary>
        /// Create nodes filling the given parcels
        /// </summary>
        /// <param name="parcels"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        protected abstract IEnumerable<KeyValuePair<Parcel, ProceduralScript>> CreateParcelNodes(Parcel[] parcels, float height);
    }
}
