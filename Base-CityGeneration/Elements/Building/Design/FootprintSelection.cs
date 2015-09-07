
using Base_CityGeneration.Elements.Building.Design.Spec.Markers;

namespace Base_CityGeneration.Elements.Building.Design
{
    public class FootprintSelection
    {
        public BaseMarker Marker { get; private set; }

        /// <summary>
        /// Indicates the floor which this selection creates a footprint for (i.e. the floor it is immediately below)
        /// </summary>
        public int Index { get; private set; }

        public FootprintSelection(BaseMarker marker, int index)
        {
            Marker = marker;
            Index = index;
        }
    }
}
