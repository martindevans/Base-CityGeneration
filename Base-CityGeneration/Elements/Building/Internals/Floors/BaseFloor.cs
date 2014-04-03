using Base_CityGeneration.Elements.Building.Facades;
using Base_CityGeneration.Elements.Building.Internals.VerticalFeatures;
using EpimetheusPlugins.Procedural;
using EpimetheusPlugins.Procedural.Utilities;
using EpimetheusPlugins.Scripts;
using EpimetheusPlugins.Services.CSG;
using Microsoft.Xna.Framework;
using Myre;
using Myre.Collections;

namespace Base_CityGeneration.Elements.Building.Internals.Floors
{
    /// <summary>
    /// Places a floor into a hollow section of empty space
    /// </summary>
    [Script("6F711293-666D-48DA-A250-8775C39D845A", "Basic Blank Floor")]
    public class BaseFloor
        :BaseContainedSpace, IFloor, IFacadeProvider
    {
        /// <summary>
        /// The index of this floor
        /// </summary>
        public int FloorIndex { get; set; }

        public BaseBuilding ParentBuilding { get; set; }

        public IVerticalFeature[] Overlaps { get; set; }

        public BaseFloor()
            :this(1, 10, 0.15f, 0.1f, 0, 0.1f, 0)
        {
        }

        protected BaseFloor(float minHeight = 1, float maxHeight = 10, float wallThickness = 0.15f, float floorThickness = 0.1f, float floorOffset = 0, float ceilingThickness = 0.1f, float ceilingOffset = 0)
            :base(minHeight, maxHeight, wallThickness, floorThickness, floorOffset, ceilingThickness, ceilingOffset)
        {
        }

        protected override ICsgShape CreateCeilingBrush(ICsgFactory geometry, INamedDataCollection hierarchicalParameters, Prism bounds)
        {
            var brush = base.CreateCeilingBrush(geometry, hierarchicalParameters, bounds);
            return SubtractOverlaps(geometry, hierarchicalParameters, bounds, brush);
        }

        protected override ICsgShape CreateFloorBrush(ICsgFactory geometry, INamedDataCollection hierarchicalParameters, Prism bounds)
        {
            var brush = base.CreateFloorBrush(geometry, hierarchicalParameters, bounds);
            return SubtractOverlaps(geometry, hierarchicalParameters, bounds, brush);
        }

        private ICsgShape SubtractOverlaps(ICsgFactory geometry, INamedDataProvider hierarchicalParameters, Prism bounds, ICsgShape brush)
        {
            //Subtract out a hole for each overlap
            foreach (var overlap in Overlaps)
            {
                //Transform points from local space of overlap into local space of this floor
                var points = overlap.Bounds.Footprint;
                var w = overlap.WorldTransformation * InverseWorldTransformation;
                Vector2.Transform(points, ref w, points);

                brush = brush.Subtract(geometry.CreatePrism(hierarchicalParameters.GetValue(new TypedName<string>("material"), true), points, bounds.Height));
            }

            return brush;
        }

        #region provide facades for rooms on this floor
        public virtual IFacade CreateFacade(IFacadeOwner owner, Walls.Section section)
        {
            //By default return nothing which defers the choice to either:
            // 1. Further up the tree
            // 2. The room makes it's own choice
            return null;
        }

        public virtual void ConfigureFacade(IFacade facade, IFacadeOwner owner)
        {
        }
        #endregion
    }
}
