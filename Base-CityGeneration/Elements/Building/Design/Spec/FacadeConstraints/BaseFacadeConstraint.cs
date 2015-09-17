using System.Numerics;

namespace Base_CityGeneration.Elements.Building.Design.Spec.FacadeConstraints
{
    public abstract class BaseFacadeConstraint
    {
        /// <summary>
        /// Check if this constraint is not violated for a given facade
        /// </summary>
        /// <param name="floor">The floor we're checking</param>
        /// <param name="neighbours">Neighbour information around the base footprint of this building</param>
        /// <param name="edgeStart">The 2D start location of the wall we're applying a facade to</param>
        /// <param name="edgeEnd">The 2D end location of the wall we're applying a facade to</param>
        /// <returns></returns>
        public abstract bool Check(FloorSelection floor, BuildingSideInfo[] neighbours, Vector2 edgeStart, Vector2 edgeEnd);

        internal abstract class BaseContainer
        {
            public abstract BaseFacadeConstraint Unwrap();
        }
    }
}
