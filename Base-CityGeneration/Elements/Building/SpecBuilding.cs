using System.Collections.Generic;
using System.Numerics;
using Base_CityGeneration.Elements.Building.Design;
using EpimetheusPlugins.Procedural;
using EpimetheusPlugins.Scripts;
using Myre.Collections;
using System;

namespace Base_CityGeneration.Elements.Building
{
    /// <summary>
    /// A building created from a design spec (generally you should not use this directly, use SpecBuildingContainer)
    /// </summary>
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
            _design = internals.Externals(Random, hierarchicalParameters, a => ScriptReference.Find(a).Random((Func<double>)Random), new float[] { 0 }); //todo: Get neighbour height data

            base.Subdivide(bounds, geometry, hierarchicalParameters);
        }

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
            return Bounds.Footprint;    //todo: footprints in spec building
        }
    }
}
