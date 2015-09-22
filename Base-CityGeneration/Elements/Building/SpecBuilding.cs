using Base_CityGeneration.Elements.Building.Design;
using EpimetheusPlugins.Procedural;
using EpimetheusPlugins.Scripts;
using Myre.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

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

        public override bool Accept(Prism bounds, INamedDataProvider parameters)
        {
            return true;
        }

        public override void Subdivide(Prism bounds, ISubdivisionGeometry geometry, INamedDataCollection hierarchicalParameters)
        {
            //Get internals generated up tree in container
            var internals = hierarchicalParameters.GetValue(SpecBuildingContainer.BuildingInternalsName);

            //Generate externals, now we can get the neighbour information that we need from the surrounding containers
            _design = internals.Externals(Random, hierarchicalParameters, a => ScriptReference.Find(a).Random((Func<double>)Random), GetNeighbourInfo(bounds));

            base.Subdivide(bounds, geometry, hierarchicalParameters);
        }

        #region abstract queries
        protected override IEnumerable<FloorSelection> SelectFloors()
        {
            return _design.Floors;
        }

        protected override IEnumerable<VerticalSelection> SelectVerticals()
        {
            return _design.Verticals;
        }

        protected override IEnumerable<Footprint> SelectExternals()
        {
            return from wall in _design.Walls
                   group wall by wall.BottomIndex
                   into footprint
                   let ordered = footprint.OrderBy(a => a.FootprintIndex)
                   let shape = ordered.Select(a => a.Start).ToArray()
                   let facades = ordered.Select(a => a.Facades.ToArray()).ToArray()
                   select new Footprint(footprint.Key, shape, facades);
        }
        #endregion
    }
}
