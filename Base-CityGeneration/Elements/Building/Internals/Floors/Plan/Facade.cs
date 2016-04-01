using EpimetheusPlugins.Procedural.Utilities;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Plan
{
    public class Facade
    {
        public Facade Next { get; internal set; }
        public Facade Previous { get; internal set; }

        private readonly IRoomPlan _neighbouringRoom;
        public IRoomPlan NeighbouringRoom { get { return _neighbouringRoom; } }

        private readonly bool _isExternal;
        public bool IsExternal { get { return _isExternal; } }

        private readonly Walls.Section _section;
        public Walls.Section Section { get { return _section; } }

        public Facade(IRoomPlan other, bool external, Walls.Section section)
        {
            _neighbouringRoom = other;
            _isExternal = external;
            _section = section;
        }
    }
}
