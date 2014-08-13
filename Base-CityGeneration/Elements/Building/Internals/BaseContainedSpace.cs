using System;
using System.Linq;
using Base_CityGeneration.Elements.Building.Facades;
using EpimetheusPlugins.Procedural;
using EpimetheusPlugins.Procedural.Utilities;
using EpimetheusPlugins.Services.CSG;
using Microsoft.Xna.Framework;
using Myre;
using Myre.Collections;

namespace Base_CityGeneration.Elements.Building.Internals
{
    /// <summary>
    /// Some kind of space surrounded by walls and/or ceilings
    /// </summary>
    public abstract class BaseContainedSpace
        : ProceduralScript, IFacadeOwner
    {
        private readonly float _minHeight;
        private readonly float _maxHeight;

        protected readonly float WallThickness;
        protected readonly float FloorThickness;
        protected readonly float FloorOffset;
        protected readonly float CeilingThickness;
        protected readonly float CeilingOffset;

        protected virtual Type[] FacadeSearchEndTypes
        {
            get { return new Type[0]; }
        }

        protected BaseContainedSpace(float minHeight = 1, float maxHeight = 10, float wallThickness = 0.15f, float floorThickness = 0.1f, float floorOffset = 0, float ceilingThickness = 0.1f, float ceilingOffset = 0)
        {
            _minHeight = minHeight;
            _maxHeight = maxHeight;
            WallThickness = wallThickness;
            FloorThickness = floorThickness;
            FloorOffset = floorOffset;
            CeilingThickness = ceilingThickness;
            CeilingOffset = ceilingOffset;
        }

        public override bool Accept(Prism bounds, INamedDataProvider parameters)
        {
            return bounds.Height >= _minHeight && bounds.Height <= _maxHeight;
        }

        public override void Subdivide(Prism bounds, ISubdivisionGeometry geometry, INamedDataCollection hierarchicalParameters)
        {
            //Generate a new bounds which is the outer bound pulled in by wall thickness
            var innerBound = bounds.Footprint.Shrink(WallThickness).ToArray();

            //Place wall sections
            var sections = bounds.Footprint.Sections(innerBound).ToArray();
            for (int i = 0; i < sections.Length; i++)
            {
                var section = sections[i];

                var prev = sections[(i + sections.Length - 1) % sections.Length];
                var next = sections[(i + 1) % sections.Length];

                var s = section.IsCorner ? CreateCornerBrush(geometry, hierarchicalParameters, prev, section, next, bounds.Height) : CreateFacadeBrush(geometry, hierarchicalParameters, prev, section, next, bounds.Height);
                if (s != null)
                    geometry.Union(s);
            }

            //Place floor                                                                             
            var floor = CreateFloorBrush(geometry, hierarchicalParameters, new Prism(bounds.Height, innerBound));
            if (floor != null)
                geometry.Union(floor);

            //Place floor
            var ceiling = CreateCeilingBrush(geometry, hierarchicalParameters, new Prism(bounds.Height, innerBound));
            if (ceiling != null)
                geometry.Union(ceiling);
        }

        /// <summary>
        /// A wall is a series of corners and facades. Create a brush to place into a corner element
        /// </summary>
        /// <param name="geometry"></param>
        /// <param name="hierarchicalParameters"></param>
        /// <param name="previous"></param>
        /// <param name="corner"></param>
        /// <param name="next"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        protected virtual ICsgShape CreateCornerBrush(ICsgFactory geometry, INamedDataCollection hierarchicalParameters, Walls.Section previous, Walls.Section corner, Walls.Section next, float height)
        {
            return geometry.CreatePrism(hierarchicalParameters.GetValue(new TypedName<string>("material"), true), new Vector2[]
            {
                corner.A, corner.B, corner.C, corner.D
            }, height);
        }

        /// <summary>
        /// A wall is a series of corners and facades. Create a brush to place into a facade element.
        /// </summary>
        /// <param name="geometry"></param>
        /// <param name="hierarchicalParameters"></param>
        /// <param name="previous"></param>
        /// <param name="facade"></param>
        /// <param name="next"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        protected virtual ICsgShape CreateFacadeBrush(ICsgFactory geometry, INamedDataCollection hierarchicalParameters, Walls.Section previous, Walls.Section facade, Walls.Section next, float height)
        {
            var f = this.FindFacade(facade, FacadeSearchEndTypes);
            if (f != null)
                return null;    //No need to create a facade brush, ancestor is providing a facade element instead

            return geometry.CreatePrism(hierarchicalParameters.GetValue(new TypedName<string>("material")), new Vector2[]
            {
                facade.A, facade.B, facade.C, facade.D
            }, height);
        }

        /// <summary>
        /// Create a brush to fill in the floor of this node. Return null for no floor
        /// </summary>
        /// <param name="geometry"></param>
        /// <param name="hierarchicalParameters"></param>
        /// <param name="bounds"></param>
        /// <returns></returns>
        protected virtual ICsgShape CreateFloorBrush(ICsgFactory geometry, INamedDataCollection hierarchicalParameters, Prism bounds)
        {
            return geometry.CreatePrism(hierarchicalParameters.GetValue(new TypedName<string>("material")), bounds.Footprint, FloorThickness)
                           .Transform(Matrix.CreateTranslation(0, -bounds.Height / 2 + FloorThickness / 2 + FloorOffset, 0));
        }

        /// <summary>
        /// Create a brush to fill in the ceiling of this node. Return null for no ceiling
        /// </summary>
        /// <param name="geometry"></param>
        /// <param name="hierarchicalParameters"></param>
        /// <param name="bounds"></param>
        /// <returns></returns>
        protected virtual ICsgShape CreateCeilingBrush(ICsgFactory geometry, INamedDataCollection hierarchicalParameters, Prism bounds)
        {
            return geometry.CreatePrism(hierarchicalParameters.GetValue(new TypedName<string>("material")), bounds.Footprint, CeilingThickness)
                           .Transform(Matrix.CreateTranslation(0, bounds.Height / 2 - CeilingThickness / 2 - CeilingOffset, 0));
        }
    }
}
