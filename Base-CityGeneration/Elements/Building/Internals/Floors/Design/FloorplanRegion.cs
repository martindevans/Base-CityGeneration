using System;
using System.Collections.Generic;
using Base_CityGeneration.Datastructures;
using Base_CityGeneration.Elements.Building.Internals.Floors.Design.Spaces;
using Base_CityGeneration.Utilities;
using Myre.Collections;
using Vector2 = System.Numerics.Vector2;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Design
{
    public class FloorplanRegion
        : BasePolygonRegion<FloorplanRegion, Section>
    {
        private readonly List<BaseSpaceSpec> _assignedSpaces = new List<BaseSpaceSpec>(); 
        public IReadOnlyList<BaseSpaceSpec> AssignedSpaces { get { return _assignedSpaces; } }

        public float AssignedSpaceArea { get; private set; }
        public float UnassignedArea {
            get { return Area - AssignedSpaceArea; }
        }

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

        protected override Section ConstructNeighbourSection(FloorplanRegion neighbour)
        {
            return new Section(0, 1, neighbour);
        }

        protected override Section Subsection(Section section, float tStart, float tEnd)
        {
            var s = Math.Max(tStart, section.Start);
            var e = Math.Min(tEnd, section.End);

            if (section.Type == Section.Types.Neighbour)
                return new Section(s, e, section.Neighbour);
            else
                return new Section(s, e, section.Type);
        }

        public void Add(BaseSpaceSpec spec, Func<double> random, INamedDataCollection metadata)
        {
            //Save this space
            _assignedSpaces.Add(spec);

            //Update the area consumed in this space (assuming the minimum)
            AssignedSpaceArea += spec.MinArea(random, metadata);
        }
    }

    public class Side
        : BasePolygonRegion<FloorplanRegion, Section>.Side
    {
        public Side(IReadOnlyList<Section> sections, Vector2 end, Vector2 start)
            : base(sections, end, start)
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
