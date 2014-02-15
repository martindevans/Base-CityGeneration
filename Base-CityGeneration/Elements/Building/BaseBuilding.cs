using System;
using Base_CityGeneration.Elements.Generic;
using EpimetheusPlugins.Procedural;
using Microsoft.Xna.Framework;
using Myre;
using Myre.Collections;

namespace Base_CityGeneration.Elements.Building
{
    public abstract class BaseBuilding
        :ProceduralScript, IGrounded
    {
        public float GroundHeight { get; set; }

        private readonly int _minFloors;
        private readonly int _maxFloors;
        private readonly int _minBasementFloors;
        private readonly int _maxBasementFloors;
        private readonly float _minFloorHeight;
        private readonly float _maxFloorHeight;

        public BaseBuilding(int minFloors, int maxFloors, int minBasementFloors, int maxBasementFloors, float minFloorHeight, float maxFloorHeight)
        {
            _minFloors = minFloors;
            _maxFloors = maxFloors;
            _minBasementFloors = minBasementFloors;
            _maxBasementFloors = maxBasementFloors;
            _minFloorHeight = minFloorHeight;
            _maxFloorHeight = maxFloorHeight;
        }

        public override bool Accept(Prism bounds, INamedDataProvider parameters)
        {
            return
                bounds.Height / _minFloorHeight >= _minFloors;  //Can we fit the min required number of floors at the minimum allowed height into this space?
        }

        public override void Subdivide(Prism bounds, ISubdivisionGeometry geometry, INamedDataCollection hierarchicalParameters)
        {
            //Caculate how much of the available height we want to use
            int count;
            int basementCount;
            float floorHeight;
            CalculateFloorHeight(Random, _minFloors, _maxFloors, _minBasementFloors, _maxBasementFloors, _minFloorHeight, _maxFloorHeight, bounds.Height - GroundHeight, GroundHeight, out count, out basementCount, out floorHeight);

            //Fill in this space solid
            var totalHeight = floorHeight * count;
            geometry.Union(geometry.CreatePrism(hierarchicalParameters.GetValue(new TypedName<string>("material")) ?? "concrete", bounds.Footprint, totalHeight)
                .Translate(new Vector3(0, -bounds.Height / 2 + totalHeight / 2 + GroundHeight - basementCount * floorHeight, 0))
            );

            CreateCrossFloorFeatures(bounds, count, basementCount, floorHeight);

            CreateFloors(bounds, count, basementCount, floorHeight);
        }

        /// <summary>
        /// Create features which cross severa floors (e.g. stairwells, lift shafts, utility shafts)
        /// </summary>
        /// <param name="bounds">The bounds of the entire building</param>
        /// <param name="floors">The number of above ground floors</param>
        /// <param name="basements">The number of below ground basements</param>
        /// <param name="floorHeight">The height of each floor</param>
        protected abstract void CreateCrossFloorFeatures(Prism bounds, int floors, int basements, float floorHeight);

        /// <summary>
        /// Create nodes for all the floors of this building
        /// </summary>
        /// <param name="bounds">The bounds of the entire building</param>
        /// <param name="floors">The number of above ground floors</param>
        /// <param name="basements">The number of below ground basements</param>
        /// <param name="floorHeight">The height of each floor</param>
        protected abstract void CreateFloors(Prism bounds, int floors, int basements, float floorHeight);

        protected float VerticalOffset(Prism bounds, int floor, float floorHeight, int basementCount)
        {
            return floorHeight / 2 + floorHeight * (floor - basementCount) + bounds.Height / 2 - GroundHeight;
        }

        internal static void CalculateFloorHeight(Func<double> random, int minFloors, int maxFloors, int minBasements, int maxBasements, float minFloorHeight, float maxFloorHeight, float heightAboveGround, float heightBelowGround, out int floors, out int basements, out float floorHeight)
        {
            //what's max floor height whilst still fitting all the floors in the available space?
            float maxHeight = Math.Min(Math.Min(heightAboveGround / minFloors, heightBelowGround / minBasements), maxFloorHeight);

            //Pick a random height in the allowable range
            floorHeight = RandomUtilities.RandomSingle(random, minFloorHeight, maxHeight);

            //How many floors of this height can we fit in the building?
            var maxFittingFloors = Math.Min((int)(heightAboveGround / floorHeight), maxFloors);
            var maxFittingBasements = Math.Min((int)(heightBelowGround / floorHeight), maxBasements);

            //Ok, so how many floors will we have?
            floors = RandomUtilities.RandomInteger(random, minFloors, maxFittingFloors);
            basements = RandomUtilities.RandomInteger(random, minBasements, maxFittingBasements);
        }
    }
}
