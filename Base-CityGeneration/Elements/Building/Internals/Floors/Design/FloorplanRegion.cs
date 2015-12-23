using System;
using System.Collections.Generic;
using System.Linq;
using Base_CityGeneration.Datastructures;
using Base_CityGeneration.Elements.Building.Internals.Floors.Design.Spaces;
using Base_CityGeneration.Utilities;
using Myre.Collections;
using System.Numerics;
using SquarifiedTreemap.Model;
using SquarifiedTreemap.Model.Input;
using SquarifiedTreemap.Model.Output;
using SwizzleMyVectors.Geometry;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Design
{
    public class FloorplanRegion
        : BasePolygonRegion<FloorplanRegion, Section>, ISpaceSpecProducer
    {
        #region field and properties
        private readonly List<BaseSpaceSpec> _requiredAssignedSpaces = new List<BaseSpaceSpec>();
        public IReadOnlyList<BaseSpaceSpec> RequiredAssignedSpaces { get { return _requiredAssignedSpaces; } }

        private readonly List<BaseSpaceSpec> _optionalAssignedSpaces = new List<BaseSpaceSpec>();
        public IReadOnlyList<BaseSpaceSpec> OptionalAssignedSpaces { get { return _optionalAssignedSpaces; } }

        public IEnumerable<BaseSpaceSpec> AssignedSpaces { get { return _requiredAssignedSpaces.Concat(_optionalAssignedSpaces); } }

        public float AssignedSpaceArea { get; private set; }
        public float UnassignedArea { get { return Area - AssignedSpaceArea; } }
        #endregion

        #region construction
        internal FloorplanRegion(IReadOnlyList<Side> shape)
            : base(shape)
        {
        }

        internal FloorplanRegion(IReadOnlyList<Side> shape, OABR oabr)
            : base(shape, oabr)
        {
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

        public void Add(BaseSpaceSpec spec, bool required, Func<double> random, INamedDataCollection metadata)
        {
            //Save this space
            (required ? _requiredAssignedSpaces : _optionalAssignedSpaces).Add(spec);

            //Update the area consumed in this space (assuming the minimum)
            AssignedSpaceArea += spec.MinArea(random, metadata);
        }

        IEnumerable<BaseSpaceSpec> ISpaceSpecProducer.Produce(bool required, Func<double> random, INamedDataCollection metadata)
        {
            return (required ? _requiredAssignedSpaces : _optionalAssignedSpaces).SelectMany(r => r.Produce(required, random, metadata));
        }

        public IEnumerable<object> LayoutSpaces(Func<double> random, INamedDataCollection metadata)
        {
            //Create nodes for all the rooms
            var treemapInput = new Tree<RoomTreemapNode>.Node();
            foreach (var assignedSpace in AssignedSpaces)
                treemapInput.Add(new Tree<RoomTreemapNode>.Node(new RoomTreemapNode(assignedSpace, random, metadata)));

            //Assign extra space to rooms which are not yet max area
            var unassignedArea = UnassignedArea;
            while (unassignedArea > 0)
            {
                //How many spaces can we assign more space to?
                var candidates = treemapInput.Count(a => a.Value.Area < a.Value.MaxArea);
                if (candidates == 0)
                    break;

                //Increase the area of each space (make sure not to exceed max)
                var step = unassignedArea / candidates;
                foreach (var space in treemapInput.Where(a => a.Value.Area < a.Value.MaxArea))
                {
                    if (space.Value.Area + step > space.Value.MaxArea)
                    {
                        unassignedArea -= (space.Value.MaxArea - space.Value.Area);
                        space.Value.Area = space.Value.MaxArea;
                    }
                    else
                    {
                        unassignedArea -= step;
                        space.Value.Area += step;
                    }
                }
            }

            //Lay out rooms using treemapping algorithm (treemap is overkill since this is a one level tree, but who cares?)
            var tree = Treemap<RoomTreemapNode>.Build(new BoundingRectangle(OABR.Min, OABR.Max), new Tree<RoomTreemapNode>(treemapInput));

            foreach (var space in AssignedSpaces)
            {
                
            }

            throw new NotImplementedException();
        }
    }

    internal class RoomTreemapNode
        : ITreemapNode
    {
        public BaseSpaceSpec Space { get; private set; }

        public float Area { get; set; }

        public float MinArea { get; private set; }
        public float MaxArea { get; private set; }

        public RoomTreemapNode(BaseSpaceSpec assignedSpace, Func<double> random, INamedDataCollection metadata)
        {
            Space = assignedSpace;

            MinArea = assignedSpace.MinArea(random, metadata);
            MaxArea = assignedSpace.MaxArea(random, metadata);

            Area = MinArea;
        }

        float? ITreemapNode.Area
        {
            get { return Area; }
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
