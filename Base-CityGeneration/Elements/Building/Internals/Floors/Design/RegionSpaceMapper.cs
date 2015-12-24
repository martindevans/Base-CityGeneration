using System;
using System.Collections.Generic;
using System.Numerics;
using Base_CityGeneration.Elements.Building.Internals.Floors.Design.Spaces;
using Myre.Collections;
using SquarifiedTreemap.Model;
using SquarifiedTreemap.Model.Input;
using SquarifiedTreemap.Model.Output;
using SwizzleMyVectors.Geometry;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Design
{
    internal class RegionSpaceMapper
    {
        private readonly BoundingRectangle _bounds;

        public RegionSpaceMapper(BoundingRectangle bounds)
        {
            _bounds = bounds;
        }

        public Treemap<RoomTreemapNode> Map(IEnumerable<RoomTreemapNode> spaces)
        {
            var root = new Tree<RoomTreemapNode>.Node();

            //Each node can either stack up next to the previous, or switch direction and occupy some of the remaining space
            //Add every node to a common parent, if we change which way around is best, add a new node to the tree and add all future nodes to that
            bool? currentSplitVertical = null;
            var addTo = root;
            var remainingSpace = _bounds;
            foreach (var node in spaces)
            {
                //measure aspect ratio for vertical and horizontal fit into remaining space
                var szVert = remainingSpace.Extent.Y / node.Area;
                var arVert = remainingSpace.Extent.Y * szVert;
                var szHorz = remainingSpace.Extent.X / node.Area;
                var arHorz = remainingSpace.Extent.X * szHorz;

                //Choose the best split direction
                var splitVert = arVert < arHorz;

                //If it's changed create a new node and attach the old one to the parent
                if (!currentSplitVertical.HasValue || splitVert != currentSplitVertical.Value)
                {
                    var inner = new Tree<RoomTreemapNode>.Node();
                    addTo.Add(inner);
                    addTo = inner;

                    currentSplitVertical = splitVert;
                }

                addTo.Add(new Tree<RoomTreemapNode>.Node(node));

                remainingSpace = new BoundingRectangle(remainingSpace.Min, remainingSpace.Max - (splitVert ? new Vector2(szVert, 0) : new Vector2(0, szHorz)));
            }


            return Treemap<RoomTreemapNode>
                .Build(_bounds, new Tree<RoomTreemapNode>(root));
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
}
