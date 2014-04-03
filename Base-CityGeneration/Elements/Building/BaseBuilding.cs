using System;
using System.Collections.Generic;
using System.Linq;
using Base_CityGeneration.Elements.Building.Internals.Floors;
using Base_CityGeneration.Elements.Building.Internals.VerticalFeatures;
using Base_CityGeneration.Elements.Generic;
using Base_CityGeneration.Parcelling;
using EpimetheusPlugins.Procedural;
using Myre.Collections;

namespace Base_CityGeneration.Elements.Building
{
    public abstract class BaseBuilding
        :ProceduralScript, IGrounded, IParcelElement<BaseBuilding>
    {
        public float GroundHeight { get; set; }

        private readonly int _minFloors;
        private readonly int _maxFloors;
        private readonly int _minBasementFloors;
        private readonly int _maxBasementFloors;
        private readonly float _minFloorHeight;
        private readonly float _maxFloorHeight;

        public Parcel<BaseBuilding> Parcel { get; set; }

        protected BaseBuilding(int minFloors, int maxFloors, int minBasementFloors, int maxBasementFloors, float minFloorHeight, float maxFloorHeight)
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

            var verticals = CreateCrossFloorFeatures(bounds, count, basementCount, floorHeight).ToArray();

            //Create floors
            var floors = CreateFloors(bounds, count, basementCount, floorHeight, verticals).ToArray();

            //Associate data with floors
            for (int i = 0; i < floors.Length; i++)
            {
                if (floors[i] != null)
                {
                    floors[i].FloorIndex = i;
                    floors[i].ParentBuilding = this;
                    floors[i].Overlaps = verticals.Where(a => a.BottomFloorIndex <= i && a.TopFloorIndex >= i).ToArray();
                }
            }
        }

        /// <summary>
        /// Create features which cross several floors (e.g. stairwells, lift shafts, utility shafts)
        /// </summary>
        /// <param name="bounds">The bounds of the entire building</param>
        /// <param name="floors">The number of above ground floors</param>
        /// <param name="basements">The number of below ground basements</param>
        /// <param name="floorHeight">The height of each floor</param>
        protected abstract IEnumerable<IVerticalFeature> CreateCrossFloorFeatures(Prism bounds, int floors, int basements, float floorHeight);

        /// <summary>
        /// Create nodes for all the floors of this building
        /// </summary>
        /// <param name="bounds">The bounds of the entire building</param>
        /// <param name="floors">The number of above ground floors</param>
        /// <param name="basements">The number of below ground basements</param>
        /// <param name="floorHeight">The height of each floor</param>
        /// <param name="verticals">Vertical features which have been established in this building</param>
        protected abstract IEnumerable<IFloor> CreateFloors(Prism bounds, int floors, int basements, float floorHeight, IVerticalFeature[] verticals);

        /// <summary>
        /// Calculate the vertical offset for a floor child node to be created at
        /// </summary>
        /// <param name="buildingBounds">The bounds of the building</param>
        /// <param name="floor">The index of the floor (starting at zero)</param>
        /// <param name="floorHeight">The height of floors in this building</param>
        /// <param name="basementCount">The number of basements (floors below the value of the 'GroundHeight' property)</param>
        /// <returns></returns>
        protected float FloorVerticalOffset(Prism buildingBounds, int floor, float floorHeight, int basementCount)
        {
            return floorHeight / 2 + floorHeight * (floor - basementCount) + buildingBounds.Height / 2 - GroundHeight;
        }

        /// <summary>
        /// Calculate the offset and height of a vertical offset which starts and ends at the specified floors
        /// </summary>
        /// <param name="buildingBounds">The bounds of the building</param>
        /// <param name="startFloor">The index of the lowest floor this element overlaps</param>
        /// <param name="endFloor">The index of the highest floor this element overlaps</param>
        /// <param name="floorHeight">The height of floors in this building</param>
        /// <param name="basementCount">The number of basements (floors below the value of the 'GroundHeight' property)</param>
        /// <param name="height">The height of the vertical element</param>
        /// <param name="offset">The vertical offset of this element</param>
        /// <returns></returns>
        protected void VerticalElementVerticalOffset(Prism buildingBounds, int startFloor, int endFloor, float floorHeight, int basementCount, out float height, out float offset)
        {
            var bottom = FloorVerticalOffset(buildingBounds, startFloor, floorHeight, basementCount) - floorHeight / 2f;
            var top = FloorVerticalOffset(buildingBounds, endFloor, floorHeight, basementCount) + floorHeight / 2f;

            height = top - bottom;
            offset = top * 0.5f + bottom * 0.5f;
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
