using System.Collections.Generic;
using System.Linq;

namespace Base_CityGeneration.Elements.Building.Design
{
    public class Design
    {
        /// <summary>
        /// Floors of this building (ordered bottom to top)
        /// </summary>
        public IEnumerable<FloorSelection> Floors { get; private set; }

        /// <summary>
        /// Footprint selectors in this building
        /// </summary>
        public IEnumerable<FootprintSelection> Footprints { get; private set; }

        /// <summary>
        /// Vertical elements to place within this building
        /// </summary>
        public IEnumerable<VerticalSelection> Verticals { get; private set; }

        /// <summary>
        /// Facades to place around this building, in the same order as the "neighbour heights" supplied to the select method
        /// </summary>
        public IEnumerable<IEnumerable<FacadeSelection>> Facades { get; private set; }

        public Design(IEnumerable<FloorSelection> floors, IEnumerable<FootprintSelection> footprints, IEnumerable<VerticalSelection> verticals, IEnumerable<IEnumerable<FacadeSelection>> facades)
        {
            Floors = floors.OrderBy(a => a.Index).ToArray();
            Footprints = footprints.ToArray();
            Verticals = verticals.ToArray();

            Facades = facades.Select(a => a.ToArray()).ToArray();
        }
    }
}
