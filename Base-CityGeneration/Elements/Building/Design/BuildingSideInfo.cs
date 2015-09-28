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
            /// <summary>
            /// Start distance of this neighbour along the parent edge
            /// </summary>
            public float Start { get; private set; }

            /// <summary>
            /// End distance of this neighbour along the parent edge
            /// </summary>
            public float End { get; private set; }

            /// <summary>
            /// Height of this neighbour
            /// </summary>
            public float Height { get; private set; }

            /// <summary>
            /// Set of resources accessible from this neighbour
            /// </summary>
            public IReadOnlyList<Resource> Resources { get; private set; }

            public NeighbourInfo(float start, float end, float height, IReadOnlyList<Resource> resources)
            {
                Start = start;
                End = end;
                Height = height;
                Resources = resources;
            }

            public struct Resource
            {
                public float Bottom { get; private set; }
                public float Top { get; private set; }
                public string Type { get; private set; }

                public Resource(float bot, float top, string type)
                    : this()
                {
                    Bottom = bot;
                    Top = top;
                    Type = type;
                }
            }
        }
    }
}
