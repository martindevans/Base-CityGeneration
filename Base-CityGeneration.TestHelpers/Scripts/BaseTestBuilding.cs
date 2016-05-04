using System.Collections.Generic;
using System.Linq;
using Base_CityGeneration.Elements.Building;
using Base_CityGeneration.Elements.Building.Design;
using EpimetheusPlugins.Procedural;
using EpimetheusPlugins.Scripts;
using Myre.Collections;

namespace Base_CityGeneration.TestHelpers.Scripts
{
    public abstract class BaseTestBuilding
        : BaseBuilding
    {
        private readonly FloorSelection[] _floors;
        private readonly VerticalSelection[] _verticals;
        private readonly Footprint[] _footprints;

        protected BaseTestBuilding(FloorSelection[] floors, VerticalSelection[] verticals, Footprint[] footprints = null)
        {
            _floors = floors;
            _verticals = verticals;
            _footprints = footprints;
        }

        public override bool Accept(Prism bounds, INamedDataProvider parameters)
        {
            return true;
        }

        protected override IEnumerable<FloorSelection> SelectFloors()
        {
            return _floors;
        }

        protected override IEnumerable<VerticalSelection> SelectVerticals()
        {
            return _verticals;
        }

        protected override IEnumerable<Footprint> SelectExternals()
        {
            if (_footprints == null || _footprints.Length == 0)
            {
                var maxFloor = _floors.Max(a => a.Index);

                return new[] {
                    new Footprint(0, Bounds.Footprint,
                        Bounds.Footprint.Select(_ => new[] {
                            new FacadeSelection(ScriptReference.Find<DefaultTestFacade>().First(), 0, maxFloor)
                        }
                    ).ToArray())
                };
            }
            else
                return _footprints;
        }
    }
}
