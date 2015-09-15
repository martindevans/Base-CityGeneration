using Base_CityGeneration.Elements.Building.Design;
using EpimetheusPlugins.Procedural;
using EpimetheusPlugins.Scripts;
using Myre.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

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
            _design = internals.Externals(Random, hierarchicalParameters, a => ScriptReference.Find(a).Random((Func<double>)Random), null); //todo: Get neighbour height data

            //Generate and cache footprints
            _footprints = GenerateFootprints(_design.Walls);

            base.Subdivide(bounds, geometry, hierarchicalParameters);
        }

        #region helpers
        private IReadOnlyDictionary<int, IReadOnlyList<Vector2>> GenerateFootprints(IEnumerable<Design.Design.Wall> walls)
        {
            Dictionary<int, IReadOnlyList<Vector2>> results = new Dictionary<int, IReadOnlyList<Vector2>>();

            //Split up into a group per floor
            var levels = walls.GroupBy(a => a.BottomIndex);

            //Order by the idnex, and reconstruct the complete footprint
            foreach (var level in levels)
                results.Add(level.Key, level.OrderBy(a => a.FootprintIndex).Select(a => a.Start).ToArray());

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
            return _design.Verticals;
        }

        protected override IEnumerable<FacadeSelection> SelectFacades(IReadOnlyCollection<float> neighbourHeights)
        {
            yield break;
        }

        protected override IEnumerable<Vector2> SelectFootprint(int floor)
        {
            if (floor == 0)
                return _footprints[0];

            //Search down for next footprint
            if (floor > 0)
            {
                for (int i = floor - 1; i >= 0; i--)
                {
                    IReadOnlyList<Vector2> ft;
                    if (_footprints.TryGetValue(i, out ft))
                        return ft;
                }

                throw new InvalidOperationException(string.Format("Failed to find a footprint below floor {0}", floor));
            }

            //Floor must be < 0
            //Search up for next footprint
            for (int i = 0; i <= 0; i++)
            {
                IReadOnlyList<Vector2> ft;
                if (_footprints.TryGetValue(i, out ft))
                    return ft;
            }

            throw new InvalidOperationException(string.Format("Failed to find a footprint above floor {0}", floor));
        }
        #endregion
    }
}
