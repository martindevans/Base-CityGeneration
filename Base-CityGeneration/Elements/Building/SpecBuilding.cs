using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Base_CityGeneration.Elements.Building.Design;
using Base_CityGeneration.Elements.Building.Design.Spec.Markers;
using EpimetheusPlugins.Procedural;
using EpimetheusPlugins.Scripts;
using Myre.Collections;
using System;

namespace Base_CityGeneration.Elements.Building
{
    /// <summary>
    /// A building created from a design spec (generally you should not use this directly, use SpecBuildingContainer)
    /// </summary>
    [Script("40DA258F-477C-4D2D-A5A6-AC0D35E7C935", "Spec Building")]
    public class SpecBuilding
        : BaseBuilding
    {
        private Design.Design _design;

        private IReadOnlyDictionary<int, IReadOnlyList<Vector2>> _footprints;

        public override bool Accept(Prism bounds, INamedDataProvider parameters)
        {
            return true;
        }

        public override void Subdivide(Prism bounds, ISubdivisionGeometry geometry, INamedDataCollection hierarchicalParameters)
        {
            //Get internals generated up tree in container
            var internals = hierarchicalParameters.GetValue(SpecBuildingContainer.BuildingInternalsName);

            //Generate externals, now we can get the neighbour information that we need from the surrounding containers
            _design = internals.Externals(Random, hierarchicalParameters, a => ScriptReference.Find(a).Random((Func<double>)Random), new float[] { 0 }); //todo: Get neighbour height data

            //Generate and cache footprints
            _footprints = GenerateFootprints(bounds.Footprint, _design.Footprints, _design.Floors.Count(a => a.Index >= 0), _design.Floors.Count(a => a.Index < 0));

            base.Subdivide(bounds, geometry, hierarchicalParameters);
        }

        #region helpers
        private IReadOnlyDictionary<int, IReadOnlyList<Vector2>>  GenerateFootprints(IReadOnlyList<Vector2> initial, IEnumerable<FootprintSelection> footprints, int aboveGroundFloorCount, int belowGroundFloorCount)
        {
            Dictionary<int, IReadOnlyList<Vector2>> results = new Dictionary<int, IReadOnlyList<Vector2>>();

            //Index by floor number
            var footprintLookup = footprints.ToDictionary(a => a.Index, a => a.Marker);

            //Generate initial ground shape
            var ground = _design.Footprints.Single(a => a.Index == 0).Marker.Apply(initial);
            results.Add(0, ground);

            //Generate upwards
            var previous = ground;
            for (int i = 1; i < aboveGroundFloorCount; i++)
            {
                BaseMarker gen;
                if (footprintLookup.TryGetValue(i, out gen))
                    previous = gen.Apply(previous);
                results.Add(i, previous);
            }

            //Generate downwards
            previous = ground;
            for (int i = 1; i < belowGroundFloorCount; i++)
            {
                BaseMarker gen;
                if (footprintLookup.TryGetValue(-i, out gen))
                    previous = gen.Apply(previous);
                results.Add(-i, previous);
            }

            return results;
        }
        #endregion

        #region abstract queries
        protected override IEnumerable<FloorSelection> SelectFloors()
        {
            return _design.Floors;
        }

        protected override IEnumerable<VerticalSelection> SelectVerticals()
        {
            yield break;    //todo: verticals in spec building
        }

        protected override IEnumerable<FacadeSelection> SelectFacades()
        {
            yield break;    //todo: facades in spec building
        }

        protected override IEnumerable<Vector2> SelectFootprint(int floor)
        {
            return _footprints[floor];
        }
        #endregion
    }
}
