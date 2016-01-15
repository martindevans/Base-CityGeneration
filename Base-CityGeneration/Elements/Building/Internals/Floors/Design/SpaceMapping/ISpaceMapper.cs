using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Base_CityGeneration.Elements.Building.Internals.Floors.Design.Spaces;
using Myre.Collections;
using Base_CityGeneration.Datastructures.HalfEdge;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Design.SpaceMapping
{
    internal interface ISpaceMapper
    {
        /// <summary>
        /// Assign rectangular spaces to the given rooms
        /// </summary>
        /// <param name="region">The region to lay these spaces out in</param>
        /// <param name="spaces"></param>
        /// <param name="random"></param>
        /// <param name="metadata"></param>
        /// <returns></returns>
        Mesh<SpaceCornerVertex, SpaceWall, SpaceFace> Map(FloorplanRegion region, IEnumerable<KeyValuePair<BaseSpaceSpec, float>> spaces, Func<double> random, INamedDataCollection metadata);
    }

    public class SpaceCornerVertex
    {
    }

    public class SpaceFace
    {
        private readonly BaseSpaceSpec _spec;
        public BaseSpaceSpec Spec
        {
            get
            {
                Contract.Ensures(Contract.Result<BaseSpaceSpec>() != null);
                return _spec;
            }
        }

        public SpaceFace(BaseSpaceSpec spec)
        {
            Contract.Requires(spec != null);

            _spec = spec;
        }

        [ContractInvariantMethod]
        private void ObjectInvariants()
        {
            Contract.Invariant(_spec != null);
        }
    }

    public class SpaceWall
    {
    }
}
