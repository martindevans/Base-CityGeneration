using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Base_CityGeneration.Elements.Building.Design
{
    public class Design
    {
        /// <summary>
        /// Floors of this building (ordered bottom to top)
        /// </summary>
        public IEnumerable<FloorSelection> Floors { get; private set; }

        /// <summary>
        /// Vertical elements to place within this building
        /// </summary>
        public IEnumerable<VerticalSelection> Verticals { get; private set; }

        /// <summary>
        /// Wall sections around this building, associated with facades above them
        /// </summary>
        public IEnumerable<Wall> Walls { get; private set; }

        public Design(IEnumerable<FloorSelection> floors, IEnumerable<VerticalSelection> verticals, IEnumerable<Wall> walls)
        {
            Floors = floors.OrderBy(a => a.Index).ToArray();
            Walls = walls.ToArray();
            Verticals = verticals.ToArray();
        }

        public class Wall
        {
            public int BottomIndex { get; private set; }
            public int FootprintIndex { get; private set; }

            public Vector2 Start { get; private set; }
            public Vector2 End { get; private set; }

            public IEnumerable<FacadeSelection> Facades { get; private set; }

            public Wall(int footprintIndex, int floorIndex, Vector2 start, Vector2 end, IEnumerable<FacadeSelection> facades)
            {
                BottomIndex = floorIndex;
                FootprintIndex = footprintIndex;

                Start = start;
                End = end;

                if (facades.Any(a => a.Bottom < floorIndex))
                    throw new ArgumentException("Bottom floor of facade is below bottom floor of wall", "facades");
                Facades = facades.ToArray();
            }
        }
    }
}
