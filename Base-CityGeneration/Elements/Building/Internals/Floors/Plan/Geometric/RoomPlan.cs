using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Numerics;
using Base_CityGeneration.Geometry.Walls;
using Base_CityGeneration.Utilities.Extensions;
using SwizzleMyVectors.Geometry;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Plan.Geometric
{
    public class RoomPlan
        : IRoomPlan
    {
        #region fields and properties
        private readonly GeometricFloorplan _plan;

        private readonly IReadOnlyList<Vector2> _innerFootprint; 
        public IReadOnlyList<Vector2> InnerFootprint { get { return _innerFootprint; } }

        private readonly IReadOnlyList<Vector2> _outerFootprint;
        public IReadOnlyList<Vector2> OuterFootprint { get { return _outerFootprint; } }

        private readonly float _wallThickness;
        public float WallThickness { get { return _wallThickness; } }

        private readonly IReadOnlyList<Section> _sections;
        public IReadOnlyList<Section> Sections
        {
            get { return _sections; }
        }

        private readonly uint _id;
        private readonly IReadOnlyList<IReadOnlyList<Vector2>> _corners;

        public uint Id
        {
            get { return _id; }
        }
        #endregion

        #region constructor
        internal static bool TryCreate(GeometricFloorplan plan, IReadOnlyList<Vector2> footprint, float wallThickness, uint id, out RoomPlan room)
        {
            Contract.Requires(plan != null);
            Contract.Requires(footprint != null);
            Contract.Requires(wallThickness > 0);

            Vector2[] inner;
            IReadOnlyList<IReadOnlyList<Vector2>> corners;
            var sections = footprint.Sections(wallThickness, out inner, out corners).ToArray();
                
            //No wall sections generated means this is not a valid room shape!
            if (sections.Length == 0 || inner.Length == 0)
            {
                room = null;
                return false;
            }

            room = new RoomPlan(plan, footprint, inner, sections, corners, wallThickness, id);
            return true;
        }

        private RoomPlan(GeometricFloorplan plan, IReadOnlyList<Vector2> outer, IReadOnlyList<Vector2> inner, IReadOnlyList<Section> sections, IReadOnlyList<IReadOnlyList<Vector2>> corners, float wallThickness, uint id)
        {
            Contract.Requires(plan != null);
            Contract.Requires(outer != null);
            Contract.Requires(inner != null);
            Contract.Requires(sections != null);
            Contract.Requires(wallThickness > 0);

            _plan = plan;
            _id = id;

            _innerFootprint = inner;
            _outerFootprint = outer;
            _sections = sections;
            _corners = corners;
            _wallThickness = wallThickness;
        }

        [ContractInvariantMethod]
        private void ObjectInvariant()
        {
            Contract.Invariant(_plan != null);
            Contract.Invariant(_innerFootprint != null);
            Contract.Invariant(_outerFootprint != null);
        }
        #endregion

        public IEnumerable<Neighbour> Neighbours
        {
            get { return _plan.GetNeighbours(this); }
        }

        public IReadOnlyList<IReadOnlyList<Vector2>> GetCorners()
        {
            return _corners;
        }

        /// <summary>
        /// Get all facades surrounding this room.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Facade> GetWalls()
        {
            Contract.Ensures(Contract.Result<IEnumerable<Facade>>() != null);

            //Create a place to put results
            var results = new List<Facade>();
            //Iterate the *sections* (in a non arbitrary order)
            foreach (var section in _sections)
            {
                if (IsExternalSection(section))
                {
                    //External facade! Create one long section (marked as external)
                    results.Add(new Facade(true, section));
                }
                else
                {
                    //not external, but also no neighbours! Create one long section (marked as not external)
                    results.Add(new Facade(false, section));
                }
            }

            //Link the facades together around the room
            for (var i = 0; i < results.Count; i++)
            {
                results[i].Next = results[(i + 1) % results.Count];
                results[i].Previous = results[(i + results.Count - 1) % results.Count];
            }

            return results;
        }

        private bool IsExternalSection(Section section)
        {
            return IsExternalLineSegment(section.ExternalLineSegment);
        }

        private bool IsExternalLineSegment(LineSegment2 segment)
        {
            var longLine = segment.LongLine;
            var segmentDirection = Vector2.Normalize(longLine.Direction);

            foreach (var edge in _plan.ExternalFootprint.Segments())
            {
                var edgeDir = edge.Line.Direction;

                //Check if they're parallel
                if (Math.Abs(Vector2.Dot(edgeDir, segmentDirection)) < 0.99984769515f) //Allow 1 degree difference
                    continue;

                //Check if they're collinear
                if (Math.Abs(edge.LongLine.DistanceToPoint(segment.Start)) > WallThickness || Math.Abs(edge.LongLine.DistanceToPoint(segment.End)) > WallThickness)
                    continue;

                return true;
            }

            return false;
        }
    }
}
