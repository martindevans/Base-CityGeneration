using System;
using System.Collections.Generic;
using System.Linq;
using Base_CityGeneration.Datastructures;
using Base_CityGeneration.Elements.Building.Internals.Floors.Design.Spaces;
using Base_CityGeneration.Utilities;
using Myre.Collections;
using System.Numerics;
using System.Xml.Serialization;
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

        public IEnumerable<KeyValuePair<BoundingRectangle, BaseSpaceSpec>> LayoutSpaces(Func<double> random, INamedDataCollection metadata)
        {
            //Create a node to represent each space
            var nodes = AssignedSpaces.Select(a => new RoomTreemapNode(a, random, metadata)).ToArray();

            //Assign extra space to rooms which are not yet max area
            AssignAdditionalArea(nodes);

            //Iteratively layout spaces to minimise aspect ratio
            var treemap = new RegionSpaceMapper(new BoundingRectangle(OABR.Min, OABR.Max)).Map(nodes);

            return from space in WalkTree(treemap.Root)
                   select new KeyValuePair<BoundingRectangle, BaseSpaceSpec>(space.Bounds, space.Value.Space);
        }

        private static IEnumerable<Node<T>> WalkTree<T>(Node<T> root) where T : ITreemapNode
        {
            if (root.Value != null)
                yield return root;

            foreach (var node in root)
                foreach (var child in WalkTree(node))
                    yield return child;
        }

        private void AssignAdditionalArea(IReadOnlyList<RoomTreemapNode> nodes)
        {
            var unassignedArea = UnassignedArea;
            while (unassignedArea > 0)
            {
                //How many spaces can we assign more space to?
                var candidates = nodes.Count(a => a.Area < a.MaxArea);
                if (candidates == 0)
                    break;

                //Increase the area of each space (make sure not to exceed max)
                var step = unassignedArea / candidates;
                foreach (var space in nodes.Where(a => a.Area < a.MaxArea))
                {
                    if (space.Area + step > space.MaxArea)
                    {
                        unassignedArea -= (space.MaxArea - space.Area);
                        space.Area = space.MaxArea;
                    }
                    else
                    {
                        unassignedArea -= step;
                        space.Area += step;
                    }
                }
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
