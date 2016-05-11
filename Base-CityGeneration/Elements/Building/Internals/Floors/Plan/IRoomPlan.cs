using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Numerics;
using Base_CityGeneration.Elements.Building.Internals.Rooms;
using Base_CityGeneration.Geometry.Walls;
using EpimetheusPlugins.Scripts;
using SwizzleMyVectors.Geometry;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Plan
{
    [ContractClass(typeof(IRoomPlanContract))]
    public interface IRoomPlan
    {
        /// <summary>
        /// Get the plan which contains this room
        /// </summary>
        IFloorPlan Plan { get; }

        /// <summary>
        /// The outer footprint of this room (does not intersect any other rooms)
        /// </summary>
        IReadOnlyList<Vector2> OuterFootprint { get; }

        /// <summary>
        /// The inner footprint of this room (inside bounds of walls of this room, i.e. the edge of the empty space)
        /// </summary>
        IReadOnlyList<Vector2> InnerFootprint { get; }

        /// <summary>
        /// A unique ID for this room
        /// </summary>
        uint Id { get; }

        /// <summary>
        /// Get the walls surrounding this room.
        /// This is either a wall section (inner -> outer of this room) or a neighbour section (inner of this room, to inner of another room)
        /// </summary>
        /// <returns></returns>
        IEnumerable<Facade> GetWalls();

        /// <summary>
        /// Get the corner sections of the walls around this room
        /// </summary>
        /// <returns></returns>
        IReadOnlyList<IReadOnlyList<Vector2>> GetCorners();

        /// <summary>
        /// Get all the neighbour relationships for this room
        /// </summary>
        IEnumerable<Neighbour> Neighbours { get; }

        /// <summary>
        /// Sections around this room
        /// </summary>
        IReadOnlyList<Section> Sections { get; }

        /// <summary>
        /// Add a script as a possible choice for this room
        /// </summary>
        /// <param name="chance">Chance of this script being chosen (relative to all other chances). Must be positive.</param>
        /// <param name="script">Script which this room *may* use (must not be null)</param>
        void AddScript(float chance, ScriptReference script);

        /// <summary>
        /// Get all scripts (with associated probability) for this room
        /// </summary>
        IReadOnlyList<KeyValuePair<float, ScriptReference>> Scripts { get; }

        /// <summary>
        /// Get the subdivision context which is created from this plan (may be null)
        /// </summary>
        IPlannedRoom Node { get; set; }
    }

    [ContractClassFor(typeof(IRoomPlan))]
    abstract class IRoomPlanContract : IRoomPlan
    {
        public IFloorPlan Plan
        {
            get
            {
                Contract.Ensures(Contract.Result<IFloorPlan>() != null);
                return null;
            }
        }

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

        private readonly uint _id;
        public uint Id
        {
            get { return _id; }
        }

        public IEnumerable<Facade> GetWalls()
        {
            Contract.Ensures(Contract.Result<IEnumerable<Facade>>() != null);
            return null;
        }

        public IReadOnlyList<IReadOnlyList<Vector2>> GetCorners()
        {
            Contract.Ensures(Contract.Result<IReadOnlyList<IReadOnlyList<Vector2>>>() != null);
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

        public IReadOnlyList<Section> Sections
        {
            get
            {
                Contract.Ensures(Contract.Result<IReadOnlyList<Section>>() != null);
                return null;
            }
        }

        protected IRoomPlanContract(uint id)
        {
            _id = id;
        }


        public void AddScript(float chance, ScriptReference script)
        {
            Contract.Requires(script != null);
            Contract.Requires(chance > 0);
        }

        public IReadOnlyList<KeyValuePair<float, ScriptReference>> Scripts
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<KeyValuePair<float, ScriptReference>>>() != null);
                return null;
            }
        }

        public IPlannedRoom Node
        {
            get { return null; }
            set { }
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

        public static Section FindSection(this IRoomPlan room, LineSegment2 segment)
        {
            Contract.Requires(room != null);

            var segmentLongLine = segment.LongLine;

            foreach (var section in room.Sections)
            {
                var externalLongRay = section.ExternalLineSegment.LongLine;

                //Check that the lines are collinear
                if (externalLongRay.Parallelism(segmentLongLine) != Parallelism.Collinear)
                    continue;

                return section;
            }

            throw new InvalidOperationException("Cannot find a section for the given line segment");
        }
    }
}
