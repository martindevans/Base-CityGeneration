using System.Collections.Generic;
using System.Linq;
using Base_CityGeneration.Elements.Generic;
using Base_CityGeneration.Parcels.Parcelling;
using EpimetheusPlugins.Procedural;
using System.Numerics;
using Base_CityGeneration.Datastructures.Edges;
using EpimetheusPlugins.Procedural.Utilities;
using Myre.Collections;

using MathHelper = Microsoft.Xna.Framework.MathHelper;

namespace Base_CityGeneration.Elements.Blocks
{
    public abstract class BaseBlock
        :ProceduralScript, IGrounded
    {
        public float GroundHeight { get; set; }

        public override void Subdivide(Prism bounds, ISubdivisionGeometry geometry, INamedDataCollection hierarchicalParameters)
        {
            //Create land parcels
            var nodes = CreateParcelNodes(GenerateParcels(bounds.Footprint).ToArray(), bounds.Height).ToArray();

            //Set ground height (for nodes which care)
            foreach (var grounded in nodes.Select(node => node.Value).OfType<IGrounded>())
                grounded.GroundHeight = GroundHeight;

            //Build neighbour set
            var neighbours = new NeighbourSet<ISubdivisionContext>();
            foreach (var keyValuePair in nodes)
                foreach (var edge in keyValuePair.Key.Edges)
                    neighbours.Add(new LineSegment2D(edge.Start, edge.End), keyValuePair.Value);

            //Associate node with their neighbours (for nodes which care)
            foreach (var neighbour in nodes.Where(node => node.Value is INeighbour))
                ((INeighbour)neighbour.Value).Neighbours = CalculateNeighbours(neighbour, neighbours).ToArray();
        }

        private static IEnumerable<NeighbourInfo> CalculateNeighbours(KeyValuePair<Parcel, ISubdivisionContext> subject, NeighbourSet<ISubdivisionContext> nodes)
        {
            foreach (var edge in subject.Key.Edges)
            {
                var query = new LineSegment2D(edge.Start, edge.End);
                var neighbours = nodes.Neighbours(query, MathHelper.ToRadians(5), 1);
                foreach (var neighbour in neighbours)
                {
                    //Do not add self as a neighbour!
                    if (neighbour.Value.Equals(subject.Value))
                        continue;

                    yield return new NeighbourInfo(
                        neighbour.Value,
                        neighbour.Segment,
                        neighbour.SegmentOverlapStart,
                        neighbour.SegmentOverlapEnd,
                        query,
                        neighbour.QueryOverlapStart,
                        neighbour.QueryOverlapEnd
                    );
                }
            }
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
