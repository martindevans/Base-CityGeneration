using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Numerics;
using Base_CityGeneration.Elements.Building.Design;
using Base_CityGeneration.Elements.Building.Internals.Floors.Design;
using Base_CityGeneration.Elements.Building.Internals.Floors.Plan;
using Base_CityGeneration.Elements.Building.Internals.VerticalFeatures;
using Base_CityGeneration.Styles;
using EpimetheusPlugins.Procedural;
using EpimetheusPlugins.Scripts;
using Myre;
using Myre.Collections;

namespace Base_CityGeneration.Elements.Building.Internals.Floors
{
    public abstract class BaseSpecFloor
        : BaseFloor
    {
        public static readonly TypedName<FloorDesigner> FloorDesignerName = new TypedName<FloorDesigner>("floor_designer");

        private FloorDesigner _designer;

        protected BaseSpecFloor(FloorDesigner designer)
            : base(1.4f, float.MaxValue)
        {
            Contract.Requires(designer != null);

            _designer = designer;
        }

        /// <summary>
        /// Construct a spec floor which will find it's spec from the hierarchicalParameters
        /// </summary>
        /// <param name="findInMetadata">Indicates if the system should find it's spec in the metadata (must be set to true)</param>
        protected BaseSpecFloor(bool findInMetadata = false)
        {
            //This is weird; why am I taking a parameter and requiring that you set it to a known value?
            //I don't really need any parameters here, so I could leave this empty. However, if I did that
            //then this would be the *default* but I want this to be an obscure option no one uses! If you
            //use this by accident you will get an exception (telling you exactly what's wrong)

            if (findInMetadata)
                throw new ArgumentException("If this is false then no spec will be found, must be true", "findInMetadata");

            _designer = null;
        }

        public override void Subdivide(Prism bounds, ISubdivisionGeometry geometry, INamedDataCollection hierarchicalParameters)
        {
            if (_designer == null)
                _designer = hierarchicalParameters.GetValue(FloorDesignerName, false);

            base.Subdivide(bounds, geometry, hierarchicalParameters);
        }

        protected override IEnumerable<KeyValuePair<VerticalSelection, IRoomPlan>> CreateFloorPlan(IFloorPlanBuilder builder, IReadOnlyDictionary<IRoomPlan, KeyValuePair<VerticalSelection, IVerticalFeature>> overlappingVerticalElements, IReadOnlyList<ConstrainedVerticalSelection> constrainedVerticalElements)
        {
            //IReadOnlyList<IReadOnlyList<Subsection>> sections;
            //IReadOnlyList<IReadOnlyList<Vector2>> overlappingVerticals;

            //todo: TEMPORARY FLOORPLAN DETAILS!
            var sections = new[] {
                new Subsection[0],
                new Subsection[0],
                new Subsection[0],
                new Subsection[0],
                new Subsection[0],
                new Subsection[0],
                new Subsection[0],
                new Subsection[0]
            };
            var overlappingVerticals = new Vector2[][] {
            };

            _designer.Design(
                Random,
                Metadata,
                ScriptReference.Find(Random),
                builder,
                sections,
                HierarchicalParameters.InternalWallThickness(Random),
                overlappingVerticals,
                constrainedVerticalElements
            );

            return new KeyValuePair<VerticalSelection, IRoomPlan>[0];
        }
    }
}
