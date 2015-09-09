using System;
using System.Linq;
using System.Numerics;
using Base_CityGeneration.Elements.Building.Design;
using EpimetheusPlugins.Procedural;
using EpimetheusPlugins.Scripts;
using Myre;
using Myre.Collections;

namespace Base_CityGeneration.Elements.Building
{
    public class SpecBuildingContainer
        : BaseBuildingContainer
    {
        public static readonly TypedName<Design.Internals> BuildingInternalsName = new TypedName<Design.Internals>("building_internals");

        private readonly BuildingDesigner _designer;
        private Design.Internals _internals;

        public SpecBuildingContainer(BuildingDesigner designer)
            : base(designer.MaxHeight, float.MaxValue)  //We ensure that building will fit into the given space by setting *min* allowable space to *max* possible building height
        {
            _designer = designer;
        }

        public override void Subdivide(Prism bounds, ISubdivisionGeometry geometry, INamedDataCollection hierarchicalParameters)
        {
            _internals = _designer.Internals(Random, HierarchicalParameters, a => ScriptReference.Find(a).Random((Func<double>)Random));

            HierarchicalParameters.Set(BuildingInternalsName, _internals);

            base.Subdivide(bounds, geometry, hierarchicalParameters);

            var building = (SpecBuilding)CreateChild(bounds, Quaternion.Identity, Vector3.Zero, new ScriptReference(typeof(SpecBuilding)));
            //todo: set all neighbouring building containers as prerequisities of the building
        }

        protected override float CalculateHeight()
        {
            return _internals.Floors.Sum(a => a.Height);
        }
    }
}
