using Base_CityGeneration.Geometry.Walls;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Plan
{
    public class Facade
    {
        public Facade Next { get; internal set; }
        public Facade Previous { get; internal set; }

        private readonly bool _isExternal;
        public bool IsExternal { get { return _isExternal; } }

        private readonly Section _section;
        public Section Section { get { return _section; } }

        public Facade(bool external, Section section)
        {
            _isExternal = external;
            _section = section;
        }
    }
}
