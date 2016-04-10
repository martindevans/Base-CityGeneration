using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Numerics;

namespace Base_CityGeneration.Elements.Building.Design.Spec.FacadeConstraints
{
    [ContractClass(typeof(BaseFacadeConstraintContract))]
    public abstract class BaseFacadeConstraint
    {
        /// <summary>
        /// Check if this constraint is not violated for a given facade
        /// </summary>
        /// <param name="floor">The floor we're checking</param>
        /// <param name="neighbours">Neighbour information around the base footprint of this building</param>
        /// <param name="edgeStart">The 2D start location of the wall we're applying a facade to</param>
        /// <param name="edgeEnd">The 2D end location of the wall we're applying a facade to</param>
        /// <param name="bottom">Altitude of the bottom of the wall we're applying this facade to</param>
        /// <param name="top">Altitude of the top of the wall we're applying this facade to</param>
        /// <returns></returns>
        public abstract bool Check(FloorSelection floor, IReadOnlyList<BuildingSideInfo> neighbours, Vector2 edgeStart, Vector2 edgeEnd, float bottom, float top);

        internal abstract class BaseContainer
            : IUnwrappable<BaseFacadeConstraint>
        {
            public abstract BaseFacadeConstraint Unwrap();
        }
    }

    [ContractClassFor(typeof(BaseFacadeConstraint))]
    internal abstract class BaseFacadeConstraintContract : BaseFacadeConstraint
    {
        public override bool Check(FloorSelection floor, IReadOnlyList<BuildingSideInfo> neighbours, Vector2 edgeStart, Vector2 edgeEnd, float bottom, float top)
        {
            Contract.Requires(floor != null);
            Contract.Requires(neighbours != null);

            return false;
        }
    }
}
