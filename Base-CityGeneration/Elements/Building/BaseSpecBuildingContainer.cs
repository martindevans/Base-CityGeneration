using System;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Numerics;
using Base_CityGeneration.Elements.Building.Design;
using EpimetheusPlugins.Procedural;
using EpimetheusPlugins.Scripts;
using Myre;
using Myre.Collections;

namespace Base_CityGeneration.Elements.Building
{
    public abstract class BaseSpecBuildingContainer
        : BaseBuildingContainer
    {
        public static readonly TypedName<Design.Internals> BuildingInternalsName = new TypedName<Design.Internals>("building_internals");

        private readonly BuildingDesigner _designer;
        private Design.Internals _internals;

        protected BaseSpecBuildingContainer(BuildingDesigner designer)
            : base(designer.MaxHeight, float.MaxValue)  //We ensure that building will fit into the given space by setting *min* allowable space to *max* possible building height
        {
            Contract.Requires(designer != null);

            _designer = designer;
        }

        public override void Subdivide(Prism bounds, ISubdivisionGeometry geometry, INamedDataCollection hierarchicalParameters)
        {
            _internals = _designer.Internals(Random, HierarchicalParameters, ScriptReference.Find(Random));

            HierarchicalParameters.Set(BuildingInternalsName, _internals);

            base.Subdivide(bounds, geometry, hierarchicalParameters);

            //Create the node which will create the building form the spec
            var building = (SpecBuilding)CreateChild(bounds, Quaternion.Identity, Vector3.Zero, new ScriptReference(typeof(SpecBuilding)));

            //Make sure sibling container subdivide before building
            foreach (var sibling in Parent.Children.OfType<IBuildingContainer>())
                building.AddPrerequisite(sibling, false);

            //Copy neighbour data into building (from container)
            building.Neighbours = Neighbours;
        }

        protected override float CalculateHeight()
        {
            return _internals.Floors.Sum(a => a.Height);
        }
    }
}
