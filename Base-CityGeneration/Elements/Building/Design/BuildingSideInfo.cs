using System.Collections.Generic;
using System.Numerics;

namespace Base_CityGeneration.Elements.Building.Design
{
    public class BuildingSideInfo
    {
        public Vector2 EdgeStart { get; private set; }

        public Vector2 EdgeEnd { get; private set; }

        private readonly NeighbourInfo[] _neighbours;
        public IEnumerable<NeighbourInfo> Neighbours
        {
            get { return _neighbours; }
        }

        public BuildingSideInfo(Vector2 start, Vector2 end, NeighbourInfo[] neighbours)
        {
            EdgeStart = start;
            EdgeEnd = end;
            _neighbours = neighbours;
        }

        public class NeighbourInfo
        {
            public float Start { get; private set; }
            public float End { get; private set; }
            public float Height { get; private set; }

            public NeighbourInfo(float start, float end, float height)
            {
                Start = start;
                End = end;
                Height = height;
            }
        }
    }
}
