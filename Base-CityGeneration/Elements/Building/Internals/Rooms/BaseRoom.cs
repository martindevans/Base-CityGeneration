using Base_CityGeneration.Elements.Block.Parcelling;
using EpimetheusPlugins.Scripts;

namespace Base_CityGeneration.Elements.Building.Internals.Rooms
{
    [Script("E655C852-8B0E-460B-BD30-35158DA1053C", "Base Room")]
    public class BaseRoom
        : BaseContainedSpace, IRoom
    {
        public Parcel Parcel { get; set; }

        public BaseRoom()
            :this(1, 10, 0.15f, 0.1f, 0, 0.1f, 0)
        {
            
        }

        public BaseRoom(float minHeight = 1, float maxHeight = 10, float wallThickness = 0.15f, float floorThickness = 0.1f, float floorOffset = 0, float ceilingThickness = 0.1f, float ceilingOffset = 0)
            : base(minHeight, maxHeight, wallThickness, floorThickness, floorOffset, ceilingThickness, ceilingOffset)
        {
        }
    }
}
