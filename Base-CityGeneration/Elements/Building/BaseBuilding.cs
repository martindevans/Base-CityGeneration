using Base_CityGeneration.Elements.Building.Design;
using Base_CityGeneration.Elements.Building.Facades;
using Base_CityGeneration.Elements.Building.Internals.Floors;
using Base_CityGeneration.Elements.Building.Internals.VerticalFeatures;
using EpimetheusPlugins.Procedural;
using EpimetheusPlugins.Procedural.Utilities;
using Myre.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using SwizzleMyVectors;

namespace Base_CityGeneration.Elements.Building
{
    public abstract class BaseBuilding
        :ProceduralScript, IBuilding
    {
        public float GroundHeight { get; set; }

        #region floor data
        public int AboveGroundFloors
        {
            get
            {
                CheckSubdivided();
                return _floors.Count - _belowGroundFloors;
            }
        }

        private int _belowGroundFloors;
        public int BelowGroundFloors
        {
            get
            {
                CheckSubdivided();
                return _belowGroundFloors;
            }
        }

        public int TotalFloors
        {
            get
            {
                CheckSubdivided();
                return _floors.Count;
            }
        }

        private IReadOnlyDictionary<int, IFloor> _floors; 
        public IFloor Floor(int index)
        {
            CheckSubdivided();
            return _floors[index];
        }
        #endregion

        #region vertical data
        private IReadOnlyCollection<IVerticalFeature> _verticals;

        /// <summary>
        /// Get all vertical features which overlap the given floor range
        /// </summary>
        /// <param name="lowest">Bottom floor of returned verticals must be less than or equal to this</param>
        /// <param name="highest">Top floor of returned verticals must be greater than or equal to this</param>
        /// <returns></returns>
        public IEnumerable<IVerticalFeature> Verticals(int lowest, int highest)
        {
            CheckSubdivided();
            return _verticals.Where(a => a.BottomFloorIndex <= lowest && a.TopFloorIndex >= highest);
        }
        #endregion

        #region facade data
        private IReadOnlyCollection<IBuildingFacade> _facades;

        public IEnumerable<IFacade> Facades(int floor)
        {
            CheckSubdivided();
            return _facades.Where(f => f.BottomFloorIndex <= floor && f.TopFloorIndex >= floor);
        }
        #endregion

        public override void Subdivide(Prism bounds, ISubdivisionGeometry geometry, INamedDataCollection hierarchicalParameters)
        {
            //Create things
            _floors = CreateFloors(SelectFloors());
            _verticals = CreateVerticals(SelectVerticals(), _floors);
            _facades = CreateFacades(SelectFacades(bounds.Footprint.Select(a => 0f).ToArray()));    //todo: get neighbour height data

            //Set up relationship between floor and facade (facades PrerequisiteOf floor)
            foreach (var facade in _facades)
            {
                for (int i = facade.BottomFloorIndex; i < facade.TopFloorIndex; i++)
                    _floors[i].AddPrerequisite(facade, true);
            }

            //Set up relationship between floor and verticals (floor PrerequisiteOf vertical)
            foreach (var vertical in _verticals)
            {
                for (int i = vertical.BottomFloorIndex; i < vertical.TopFloorIndex; i++)
                    vertical.AddPrerequisite(_floors[i], false);
            }
        }

        private IReadOnlyDictionary<int, IFloor> CreateFloors(IEnumerable<FloorSelection> floors)
        {
            //Sanity check selection does not have two floors in the same place
            if (floors.GroupBy(a => a.Index).Any(g => g.Count() > 1))
                throw new InvalidOperationException("Attempted to create two floors with the same index");

            //Count up the number of floors below ground
            _belowGroundFloors = floors.Count(a => a.Index < 0);

            //Collection of results (floor index -> floor node)
            Dictionary<int, IFloor> result = new Dictionary<int, IFloor>();

            //calculate offset to lowest basement
            //todo: do I need to add/subtract GroundHeight to this?
            var offset = -floors.Where(a => a.Index < 0).Sum(a => a.Height);

            //Materialize selection into child nodes
            foreach (var floor in floors.OrderBy(a => a.Index))
            {
                //Get the footprint for this floor
                var footprint = SelectFootprint(floor.Index);

                //Create node
                IFloor node = (IFloor)CreateChild(
                    new Prism(floor.Height, footprint),
                    Quaternion.Identity,
                    new Vector3(0, offset + floor.Height / 2f, 0),
                    floor.Script
                );
                node.FloorIndex = floor.Index;

                //Move offset up for next floor
                offset += floor.Height;

                //Add to result collection
                result.Add(node.FloorIndex, node);
            }

            return result;
        }

        protected abstract IEnumerable<FloorSelection> SelectFloors();

        private IReadOnlyCollection<IVerticalFeature> CreateVerticals(IEnumerable<VerticalSelection> verticals, IReadOnlyDictionary<int, IFloor> floors)
        {
            if (verticals.Any(a => a.Bottom > a.Top))
                throw new InvalidOperationException("Attempted to crete a vertical element where bottom > top");

            var results = new List<IVerticalFeature>();

            foreach (var verticalSelection in verticals)
            {
                //Get all floors this feature overlaps
                IFloor[] crossedFloors = (
                    from i in Enumerable.Range(verticalSelection.Bottom, verticalSelection.Top - verticalSelection.Bottom + 1)
                    select floors[i]
                ).ToArray();

                //Calculate the intersection of all crossed floor footprints
                var intersection = IntersectionOfFootprints(crossedFloors);

                //Ask the bottom floors where this element should be placed
                var footprint = crossedFloors[0].PlaceVerticalFeature(verticalSelection, intersection, crossedFloors);

                //Transform from floor space into building space
                var transform = crossedFloors[0].InverseWorldTransformation * WorldTransformation;
                var bFootprint = footprint.Select(a => Vector3.Transform(a.X_Y(0), transform).XZ()).ToArray();

                //Clockwise wind
                if (bFootprint.Area() < 0)
                    Array.Reverse(bFootprint);

                //Calculate height
                var height = crossedFloors.Sum(a => a.Bounds.Height);

                //Create vertical element node
                var vertical = (IVerticalFeature)CreateChild(
                    new Prism(height, bFootprint),
                    Quaternion.Identity,
                    new Vector3(0, height / 2, 0),
                    verticalSelection.Script
                );
                vertical.BottomFloorIndex = verticalSelection.Bottom;
                vertical.TopFloorIndex = verticalSelection.Top;

                //Accumulate all verticals
                results.Add(vertical);
            }

            //Associate vertical elements with the floors they intersect
            foreach (var floor in _floors.Values)
                floor.Overlaps = results.Where(a => a.TopFloorIndex >= floor.FloorIndex && a.BottomFloorIndex <= floor.FloorIndex).ToArray();

            return results;
        }

        private IReadOnlyList<Vector2> IntersectionOfFootprints(IFloor[] floors)
        {
            const int SCALE = 1000;
            Clipper c = new Clipper();

            for (int i = 0; i < floors.Length; i++)
            {
                int ii = i;
                //Transform footprint from floor[i] space into floor[0] space
                var transformed = floors[i].Bounds.Footprint.Select(a => Vector3.Transform(a.X_Y(0), floors[ii].InverseWorldTransformation * floors[0].WorldTransformation));
                c.AddPolygon(
                    transformed.Select(a => new IntPoint((int)(a.X * SCALE), (int)(a.Z * SCALE))).ToList(),
                    i == 0 ? PolyType.Subject : PolyType.Clip
                );
            }

            var result = new List<List<IntPoint>>();
            c.Execute(ClipType.Intersection, result);

            return result[0].Select(a => new Vector2(a.X / (float)SCALE, a.Y / (float)SCALE)).ToArray();
        }

        protected abstract IEnumerable<VerticalSelection> SelectVerticals();

        private IReadOnlyCollection<IBuildingFacade> CreateFacades(IEnumerable<FacadeSelection> facades)
        {
            if (facades.Any(a => a.Bottom > a.Top))
                throw new InvalidOperationException("Attempted to crete a facade element where bottom > top");

            //todo: facades
            //throw new NotImplementedException("Turn selection into actual nodes");
            foreach (var facadeSelection in facades)
            {
                
            }

            return new List<IBuildingFacade>();
        }

        protected abstract IEnumerable<FacadeSelection> SelectFacades(IReadOnlyCollection<float> neighbourHeights);

        protected abstract IEnumerable<Vector2> SelectFootprint(int floor);

        #region helpers
        private void CheckSubdivided()
        {
            if (State == SubdivisionStates.NotSubdivided)
                throw new InvalidOperationException("Cannot query BaseBuilding before it is subdivided");
        }
        #endregion
    }
}
