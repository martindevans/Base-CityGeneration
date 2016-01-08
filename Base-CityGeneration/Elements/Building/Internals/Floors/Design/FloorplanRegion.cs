using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Base_CityGeneration.Datastructures;
using Base_CityGeneration.Elements.Building.Internals.Floors.Design.Spaces;
using Base_CityGeneration.Utilities;
using Myre.Collections;
using System.Numerics;
using Base_CityGeneration.Elements.Building.Internals.Floors.Design.SpaceMapping;
using SwizzleMyVectors.Geometry;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Design
{
    public class FloorplanRegion
        : BasePolygonRegion<FloorplanRegion, Section>, ISpaceSpecProducer
    {
        #region field and properties
        private readonly List<BaseSpaceSpec> _requiredAssignedSpaces = new List<BaseSpaceSpec>();
        public IReadOnlyList<BaseSpaceSpec> RequiredAssignedSpaces
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<BaseSpaceSpec>>() != null);
                return _requiredAssignedSpaces;
            }
        }

        private readonly List<BaseSpaceSpec> _optionalAssignedSpaces = new List<BaseSpaceSpec>();
        public IReadOnlyList<BaseSpaceSpec> OptionalAssignedSpaces
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<BaseSpaceSpec>>() != null);
                return _optionalAssignedSpaces;
            }
        }

        public IEnumerable<BaseSpaceSpec> AssignedSpaces
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<BaseSpaceSpec>>() != null);
                return _requiredAssignedSpaces.Concat(_optionalAssignedSpaces);
            }
        }

        public float AssignedSpaceArea { get; private set; }
        public float UnassignedArea { get { return Area - AssignedSpaceArea; } }
        #endregion

        #region construction
        internal FloorplanRegion(IReadOnlyList<Side> shape)
            : base(shape)
        {
            Contract.Requires(shape != null);
            Contract.Requires(shape.Count >= 3);
        }

        internal FloorplanRegion(IReadOnlyList<Side> shape, OABR oabr)
            : base(shape, oabr)
        {
            Contract.Requires(shape != null);
            Contract.Requires(shape.Count >= 3);
        }

        [ContractInvariantMethod]
        private void ObjectInvariants()
        {
            Contract.Invariant(_requiredAssignedSpaces != null);
            Contract.Invariant(_optionalAssignedSpaces != null);
        }

        protected override FloorplanRegion Construct(IReadOnlyList<Side> shape)
        {
            return new FloorplanRegion(shape);
        }
        #endregion

        #region sections
        protected override Section ConstructNeighbourSection(FloorplanRegion neighbour)
        {
            return new Section(0, 1, neighbour);
        }

        protected override Section Subsection(Section section, float tStart, float tEnd)
        {
            if (section.Type == Section.Types.Neighbour)
                return new Section(tStart, tEnd, section.Neighbour);
            else
                return new Section(tStart, tEnd, section.Type);
        }
        #endregion

        #region space layout
        public void Add(BaseSpaceSpec spec, bool required)
        {
            Contract.Requires(spec != null);

            //Save this space
            (required ? _requiredAssignedSpaces : _optionalAssignedSpaces).Add(spec);

            //Update the area consumed in this space (assuming the minimum)
            AssignedSpaceArea += spec.MinArea();
        }

        IEnumerable<BaseSpaceSpec> ISpaceSpecProducer.Produce(bool required, Func<double> random, INamedDataCollection metadata)
        {
            return (required ? _requiredAssignedSpaces : _optionalAssignedSpaces).SelectMany(r => r.Produce(required, random, metadata));
        }

        public IEnumerable<KeyValuePair<BoundingRectangle, BaseSpaceSpec>> LayoutSpaces(Func<double> random, INamedDataCollection metadata)
        {
            Contract.Requires(random != null);
            Contract.Requires(metadata != null);
            Contract.Ensures(Contract.Result<IEnumerable<KeyValuePair<BoundingRectangle, BaseSpaceSpec>>>() != null);

            //Create a node to represent each space
            var nodes = AssignedSpaces.Select(a => new AreaAssignment(a, random, metadata)).ToArray();

            //Assign extra space to rooms which are not yet max area
            AssignAdditionalArea(nodes, UnassignedArea);

            //Iteratively layout spaces to minimise aspect ratio
            return new Treemapper().Map(this, nodes.Select(a => new KeyValuePair<BaseSpaceSpec, float>(a.Space, a.Area)), random, metadata);
        }

        private static void AssignAdditionalArea(IReadOnlyList<AreaAssignment> nodes, double additionalArea)
        {
            while (additionalArea > 0.01f)
            {
                //How many spaces can we assign more space to?
                var candidates = nodes.Count(a => a.Area < a.MaxArea);
                if (candidates == 0)
                    break;

                //Increase the area of each space (make sure not to exceed max)
                var step = additionalArea / candidates;
                foreach (var space in nodes.Where(a => a.Area < a.MaxArea))
                {
                    if (space.Area + step > space.MaxArea)
                    {
                        additionalArea -= (space.MaxArea - space.Area);
                        space.Area = space.MaxArea;
                    }
                    else
                    {
                        additionalArea -= step;
                        space.Area += (float)step;
                    }
                }
            }
        }

        #endregion

        private class AreaAssignment
        {
            private readonly BaseSpaceSpec _space;
            /// <summary>
            /// The space this node represents
            /// </summary>
            public BaseSpaceSpec Space
            {
                get { return _space; }
            }

            /// <summary>
            /// The area assigned to this space
            /// </summary>
            public float Area { get; set; }

            private readonly float _maxArea;
            /// <summary>
            /// Maximum area this room may be
            /// </summary>
            public float MaxArea { get { return _maxArea; } }

            public AreaAssignment(BaseSpaceSpec assignedSpace, Func<double> random, INamedDataCollection metadata)
            {
                Contract.Requires(assignedSpace != null);
                Contract.Requires(random != null);
                Contract.Requires(metadata != null);

                _space = assignedSpace;

                //Generate values for upper and lower bounds of allowed area (we start area at min, since we will expand rooms later)
                Area = assignedSpace.MinArea();
                _maxArea = assignedSpace.MaxArea();
            }

            [ContractInvariantMethod]
            private void ObjectInvariants()
            {
                Contract.Invariant(_space != null);
            }
        }
    }

    public class Side
        : BasePolygonRegion<FloorplanRegion, Section>.Side
    {
        public Side(IReadOnlyList<Section> sections, Vector2 end, Vector2 start)
            : base(end, start, sections)
        {
        }
    }

    /// <summary>
    /// A section of this struct (start and end specify distances along side in units of side length)
    /// </summary>
    public class Section
        : BasePolygonRegion<FloorplanRegion, Section>.Side.ISection
    {
        public float Start { get; private set; }
        public float End { get; private set; }

        public Types Type { get; private set; }

        public FloorplanRegion Neighbour { get; private set; }

        public Section(float start, float end, Types type)
        {
            Start = start;
            End = end;
            Type = type;
            Neighbour = null;
        }

        public Section(float start, float end, FloorplanRegion neighbour)
            : this(start, end, Types.Neighbour)
        {
            Neighbour = neighbour;
        }

        public enum Types
        {
            Window,
            Door,
            Neighbour
        }
    }
}
