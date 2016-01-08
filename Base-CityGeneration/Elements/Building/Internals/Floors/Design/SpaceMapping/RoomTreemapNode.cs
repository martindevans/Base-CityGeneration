using System.Diagnostics.Contracts;
using Base_CityGeneration.Elements.Building.Internals.Floors.Design.Spaces;
using SquarifiedTreemap.Model;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Design.SpaceMapping
{
    internal class RoomTreemapNode
        : ITreemapNode
    {
        private readonly BaseSpaceSpec _space;
        /// <summary>
        /// The space this node represents
        /// </summary>
        public BaseSpaceSpec Space
        {
            get { return _space; }
        }

        private readonly float _area;
        /// <summary>
        /// The area assigned to this space (used in treemap generation to choose size of rectangle)
        /// </summary>
        public float Area { get { return _area; } }

        /// <summary>
        /// How satisfied this space is with it's current position
        /// </summary>
        public float ConstraintSatisfaction { get; set; }

        public RoomTreemapNode(BaseSpaceSpec assignedSpace, float area)
        {
            Contract.Requires(assignedSpace != null);

            _space = assignedSpace;
            _area = area;
        }

        [ContractInvariantMethod]
        private void ObjectInvariants()
        {
            Contract.Invariant(_space != null);
        }

        float? ITreemapNode.Area
        {
            get { return Area; }
        }
    }
}
