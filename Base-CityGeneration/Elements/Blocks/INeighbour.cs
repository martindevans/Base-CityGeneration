﻿using System.Collections.Generic;
using EpimetheusPlugins.Procedural;
using EpimetheusPlugins.Procedural.Utilities;

namespace Base_CityGeneration.Elements.Blocks
{
    /// <summary>
    /// Indicates that this node is surrounded by some neighbouring nodes
    /// </summary>
    public interface INeighbour
    {
        /// <summary>
        /// The nodes surrounding this node
        /// </summary>
        IReadOnlyList<NeighbourInfo> Neighbours { get; set; }
    }

    public class NeighbourInfo
    {
        public readonly ISubdivisionContext Neighbour;

        public readonly LineSegment2D NeighbourSegment;
        public readonly float NeighbourStart;
        public readonly float NeighbourEnd;

        public readonly LineSegment2D Segment;
        public readonly float Start;
        public readonly float End;

        public NeighbourInfo(ISubdivisionContext neighbour, LineSegment2D neighbourSegment, float neighbourStart, float neighbourEnd, LineSegment2D segment, float start, float end)
        {
            Neighbour = neighbour;

            NeighbourSegment = neighbourSegment;
            NeighbourStart = neighbourStart;
            NeighbourEnd = neighbourEnd;

            Segment = segment;
            Start = start;
            End = end;
        }
    }
}
