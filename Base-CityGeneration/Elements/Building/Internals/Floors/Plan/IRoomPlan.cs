using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Numerics;
using SwizzleMyVectors.Geometry;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Plan
{
    [ContractClass(typeof(IRoomPlanContract))]
    public interface IRoomPlan
    {
        /// <summary>
        /// The outer footprint of this room (does not intersect any other rooms)
        /// </summary>
        IReadOnlyList<Vector2> OuterFootprint { get; }

        /// <summary>
        /// The inner footprint of this room (inside bounds of walls of this room, i.e. the edge of the empty space)
        /// </summary>
        IReadOnlyList<Vector2> InnerFootprint { get; }

        /// <summary>
        /// Get the walls surrounding this room.
        /// This is either a wall section (inner -> outer of this room) or a neighbour section (inner of this room, to inner of another room)
        /// </summary>
        /// <returns></returns>
        IEnumerable<Facade> GetWalls();

        /// <summary>
        /// Get all the neighbour relationships for this room
        /// </summary>
        IEnumerable<Neighbour> Neighbours { get; }
    }

    [ContractClassFor(typeof(IRoomPlan))]
    abstract class IRoomPlanContract : IRoomPlan
    {
        public IReadOnlyList<Vector2> OuterFootprint
        {
            get
            {
                Contract.Ensures(Contract.Result<IReadOnlyList<Vector2>>() != null);
                return null;
            }
        }

        public IReadOnlyList<Vector2> InnerFootprint
        {
            get
            {
                Contract.Ensures(Contract.Result<IReadOnlyList<Vector2>>() != null);
                return null;
            }
        }

        public IEnumerable<Facade> GetWalls()
        {
            Contract.Ensures(Contract.Result<IEnumerable<Facade>>() != null);
            return null;
        }

        public IEnumerable<Neighbour> Neighbours
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<Neighbour>>() != null);
                return null;
            }
        }
    }

    public static class IRoomPlanExtensions
    {
        public static LineSegment2 GetEdge(this IRoomPlan room, uint index)
        {
            Contract.Requires(room != null);

            var idx = ((int)index) % room.OuterFootprint.Count;
            return new LineSegment2(room.OuterFootprint[idx], room.OuterFootprint[(idx + 1) % room.OuterFootprint.Count]);
        }

        public static IEnumerable<LineSegment2> Edges(this IRoomPlan room)
        {
            Contract.Requires(room != null);
            Contract.Ensures(Contract.Result<IEnumerable<LineSegment2>>() != null);

            return Edges(room.OuterFootprint);
        }

        internal static IEnumerable<LineSegment2> Edges(IReadOnlyList<Vector2> points)
        {
            Contract.Requires(points != null);
            Contract.Ensures(Contract.Result<IEnumerable<LineSegment2>>() != null);

            return points
                .Select((t, i) => new LineSegment2(t, points[(i + 1) % points.Count]));
        }
    }
}
