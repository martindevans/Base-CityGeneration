using System.Collections.ObjectModel;
using Base_CityGeneration.Elements.Building.Facades;
using Base_CityGeneration.Elements.Building.Internals.Floors;
using Base_CityGeneration.Elements.Building.Internals.Floors.Selection;
using Base_CityGeneration.Elements.Building.Internals.VerticalFeatures;
using EpimetheusPlugins.Procedural;
using Myre.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

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

        private ReadOnlyCollection<IFloor> _floors; 
        public IFloor Floor(int index)
        {
            CheckSubdivided();
            return _floors[index + BelowGroundFloors];
        }
        #endregion

        #region vertical data
        private ReadOnlyCollection<IVerticalFeature> _verticals;

        public IEnumerable<IVerticalFeature> Verticals(int lowest, int highest)
        {
            CheckSubdivided();
            return _verticals.Where(a => a.BottomFloorIndex <= lowest && a.TopFloorIndex >= highest);
        }
        #endregion

        #region facade data
        private ReadOnlyCollection<IBuildingFacade> _facades;

        public IEnumerable<IFacade> Facades(int floor)
        {
            CheckSubdivided();
            return _facades.Where(f => f.BottomFloorIndex <= floor && f.TopFloorIndex >= floor);
        }
        #endregion

        public override void Subdivide(Prism bounds, ISubdivisionGeometry geometry, INamedDataCollection hierarchicalParameters)
        {
            //create thigns
            //_facades = CreateFacades(SelectFacades());
            _floors = CreateFloors(SelectFloors());
            _verticals = CreateVerticals(SelectVerticals());

            //Set up relationship between floor and facade (facades PrerequisiteOf floor)
            foreach (var facade in _facades)
            {
                for (int i = facade.BottomFloorIndex; i < facade.TopFloorIndex; i++)
                    _floors[i + _belowGroundFloors].AddPrerequisite(facade, true);
            }

            //Set up relationship between floor and verticals (floor PrerequisiteOf vertical)
            foreach (var vertical in _verticals)
            {
                for (int i = vertical.BottomFloorIndex; i < vertical.TopFloorIndex; i++)
                    vertical.AddPrerequisite(_floors[i + _belowGroundFloors], false);
            }
        }

        private ReadOnlyCollection<IFloor> CreateFloors(IEnumerable<FloorSelection> floors)
        {
            //Sanity check selection does not have two floors in the same place
            if (floors.GroupBy(a => a.Index).Any(g => g.Count() > 1))
                throw new InvalidOperationException("Attempted to create two floors with the same index");

            //Count up the number of floors below ground
            _belowGroundFloors = floors.Count(a => a.Index < 0);

            //Materialize selection into child nodes
            throw new NotImplementedException("Turn selection into actual nodes");
        }

        protected abstract IEnumerable<FloorSelection> SelectFloors();

        private ReadOnlyCollection<IVerticalFeature> CreateVerticals(IEnumerable<VerticalSelection> verticals)
        {
            if (verticals.Any(a => a.Bottom > a.Top))
                throw new InvalidOperationException("Attempted to crete a vertical element where bottom > top");

            throw new NotImplementedException("Turn selection into actual nodes");
        }

        protected abstract IEnumerable<VerticalSelection> SelectVerticals();

        //private ReadOnlyCollection<IBuildingFacade> CreateFacades(IEnumerable<FacadeSelection> selectFacades)
        //{
        //    throw new NotImplementedException("Turn selection into actual nodes");
        //}

        //protected abstract IEnumerable<FacadeSelection> SelectFacades();

        #region helpers
        private void CheckSubdivided()
        {
            if (State == SubdivisionStates.NotSubdivided)
                throw new InvalidOperationException("Cannot query BaseBuilding before it is subdivided");
        }
        #endregion
    }
}
