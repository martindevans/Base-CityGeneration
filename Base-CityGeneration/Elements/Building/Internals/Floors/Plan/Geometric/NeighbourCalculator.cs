using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Base_CityGeneration.Datastructures.Edges;
using SwizzleMyVectors.Geometry;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Plan.Geometric
{
    internal class NeighbourCalculator
    {
        #region fields/properties
        internal const float SAME_POINT_EPSILON = 0.1f;
        internal const float SAME_POINT_EPSILON_SQR = SAME_POINT_EPSILON * SAME_POINT_EPSILON;

        private readonly GeometricFloorplan _plan;

        public bool Dirty { get; set; }

        private Dictionary<IRoomPlan, List<Neighbour>> _neighbours;

        public IEnumerable<Neighbour> this[IRoomPlan key]
        {
            get
            {
                Contract.Requires(key != null);
                Contract.Ensures(Contract.Result<IEnumerable<Neighbour>>() != null);

                GenerateNeighbours();

                List<Neighbour> value;
                if (!_neighbours.TryGetValue(key, out value) || value == null)
                    throw new InvalidOperationException("Failed to find neighbour information for given room");

                return value;
            }
        }
        #endregion

        public NeighbourCalculator(GeometricFloorplan plan)
        {
            Contract.Requires(plan != null);

            _plan = plan;

            Dirty = true;
        }

        [ContractInvariantMethod]
        private void ObjectInvariants()
        {
            Contract.Invariant(_plan != null);
        }

        private struct RoomPlanSegment
        {
            public readonly IRoomPlan Room;
            public readonly ushort EdgeIndex;

            public RoomPlanSegment(IRoomPlan room, ushort edgeIndex)
            {
                EdgeIndex = edgeIndex;
                Room = room;
            }
        }

        private void GenerateNeighbours()
        {
            if (!Dirty)
                return;

            //Create a temporary neighbour set which we will add all the rooms to
            //This makes finding parallel lines fast, since we do some work up front to build the set
            var sides = CreateNeighbourSet();

            //Create output structure
            _neighbours = _plan.Rooms.ToDictionary(r => r, r => new List<Neighbour>());

            foreach (var room in _plan.Rooms)
            {
                //Get the list of neighbours for this room (we'll mutate this to store the results)
                var result = _neighbours[room];

                //Loop over each edge of the room
                for (ushort i = 0; i < room.OuterFootprint.Count; i++)
                {
                    var a = room.OuterFootprint[i];
                    var b = room.OuterFootprint[(i + 1) % room.OuterFootprint.Count];
                    var segment = new LineSegment2(a, b);
                
                    //Get the neighbours from the neighbourset (co-linear lines, which overlap segments)
                    var neighbours = sides.Neighbours(segment, 0.0174533f, GeometricFloorplan.SAFE_DISTANCE * 3f, true);

                    //Convert to output format
                    foreach (var neighbour in neighbours)
                    {
                        var startQuery = Math.Min(neighbour.QueryOverlapStart, neighbour.QueryOverlapEnd);
                        var endQuery = Math.Max(neighbour.QueryOverlapStart, neighbour.QueryOverlapEnd);

                        var startSegment = Math.Min(neighbour.SegmentOverlapStart, neighbour.SegmentOverlapEnd);
                        var endSegment = Math.Max(neighbour.SegmentOverlapStart, neighbour.SegmentOverlapEnd);

                        result.Add(new Neighbour(
                            i, room,
                            neighbour.Value.EdgeIndex, neighbour.Value.Room,
                            segment.LongLine.PointAlongLine(startQuery), startQuery,
                            segment.LongLine.PointAlongLine(endQuery), endQuery,
                            neighbour.Segment.LongLine.PointAlongLine(startSegment), startSegment,
                            neighbour.Segment.LongLine.PointAlongLine(endSegment), endSegment
                        ));
                    }
                }
            }

            Dirty = false;
        }

        private NeighbourSet<RoomPlanSegment> CreateNeighbourSet()
        {
            var sides = new NeighbourSet<RoomPlanSegment>();

            //Add the room sides
            foreach (var room in _plan.Rooms)
            {
                for (ushort i = 0; i < room.OuterFootprint.Count; i++)
                {
                    var a = room.OuterFootprint[i];
                    var b = room.OuterFootprint[(i + 1) % room.OuterFootprint.Count];
                    sides.Add(new LineSegment2(a, b), new RoomPlanSegment(room, i));
                }
            }
            return sides;
        }
    }
}
