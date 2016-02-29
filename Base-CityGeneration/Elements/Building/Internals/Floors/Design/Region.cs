using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Numerics;
using Base_CityGeneration.Utilities;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Design
{
    internal class Region
        : BasePolygonRegion<Region, Subsection>
    {
        public Region(IReadOnlyList<Side> shape)
            : base(shape)
        {
            Contract.Requires(shape != null);
            Contract.Requires(shape.Count >= 3);
        }

        #region parts factory
        protected override Subsection ConstructNeighbourSection(Region neighbour)
        {
            return new Subsection(0, 1, neighbour);
        }

        protected override Subsection Subsection(Subsection section, float tStart, float tEnd)
        {
            if (section.Type == Design.Subsection.Types.Neighbour)
                return new Subsection(tStart, tEnd, section.Neighbour);
            else
                return new Subsection(tStart, tEnd, section.Type);
        }

        protected override Region Construct(IReadOnlyList<Side> shape)
        {
            return new Region(shape);
        }
        #endregion
    }

    internal class Side
        : BasePolygonRegion<Region, Subsection>.Side
    {
        public Side(IReadOnlyList<Subsection> sections, Vector2 end, Vector2 start)
            : base(end, start, sections)
        {
        }
    }

    /// <summary>
    /// A part of a wall with some kind of feature
    /// </summary>
    public class Subsection
        : BasePolygonRegion<Region, Subsection>.Side.ISection
    {
        /// <summary>
        /// Start distance along the wall in units of wall length (0 to 1)
        /// </summary>
        public float Start { get; private set; }

        /// <summary>
        /// End distance along the wall in units of wall length (0 to 1)
        /// </summary>
        public float End { get; private set; }

        public Types Type { get; private set; }

        internal Region Neighbour { get; private set; }

        public Subsection(float start, float end, Types type)
        {
            Start = start;
            End = end;
            Type = type;
            Neighbour = null;
        }

        internal Subsection(float start, float end, Region neighbour)
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
