using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Numerics;
using SwizzleMyVectors.Geometry;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Plan
{
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

    public static class IRoomPlanExtensions
    {
        public static LineSegment2 GetEdge(this IRoomPlan room, uint index)
        {
            var idx = ((int)index) % room.OuterFootprint.Count;
            return new LineSegment2(room.OuterFootprint[idx], room.OuterFootprint[(idx + 1) % room.OuterFootprint.Count]);
        }

        public static IEnumerable<LineSegment2> Edges(this IRoomPlan room)
        {
            return Edges(room.OuterFootprint);
        }

        internal static IEnumerable<LineSegment2> Edges(IReadOnlyList<Vector2> points)
        {
            Contract.Requires(points != null);

            return points
                .Select((t, i) => new LineSegment2(t, points[(i + 1) % points.Count]));
        }
    }
}
