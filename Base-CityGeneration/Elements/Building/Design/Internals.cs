using EpimetheusPlugins.Scripts;
using Myre.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using Myre.Extensions;

namespace Base_CityGeneration.Elements.Building.Design
{
    public class Internals
    {
        private readonly BuildingDesigner _designer;

        private readonly FloorSelection[][] _aboveGroundFloorRuns;

        /// <summary>
        /// Set of floors to place above ground
        /// </summary>
        public IEnumerable<FloorSelection> AboveGroundFloors
        {
            get
            {
                foreach (var run in _aboveGroundFloorRuns)
                {
                    foreach (var floor in run)
                    {
                        yield return floor;
                    }
                }
            }
        }

        private readonly FloorSelection[][] _belowGroundFloorRuns;

        /// <summary>
        /// Set of floors to place below ground
        /// </summary>
        public IEnumerable<FloorSelection> BelowGroundFloors
        {
            get
            {
                foreach (var run in _belowGroundFloorRuns)
                {
                    foreach (var floor in run)
                    {
                        yield return floor;
                    }
                }
            }
        }

        public IEnumerable<FootprintSelection> Footprints { get; private set; }

        /// <summary>
        /// Vertical elements to place within this building
        /// </summary>
        public IEnumerable<VerticalSelection> Verticals { get; internal set; }

        internal Internals(BuildingDesigner designer, FloorSelection[][] aboveGroundFloors, FloorSelection[][] belowGroundFloors, FootprintSelection[] footprints)
        {
            _designer = designer;

            _aboveGroundFloorRuns = aboveGroundFloors;
            _belowGroundFloorRuns = belowGroundFloors;

            Footprints = footprints;
        }

        public Design Externals(Func<double> random, INamedDataCollection metadata, Func<string[], ScriptReference> finder, IEnumerable<float> neighbourHeights)
        {
            List<FacadeSelection>[] facades = new List<FacadeSelection>[neighbourHeights.Count()];
            for (int i = 0; i < facades.Length; i++)
                facades[i] = new List<FacadeSelection>();

            foreach (var run in _aboveGroundFloorRuns)
            {
                var f = _designer.SelectFacades(random, finder, run, neighbourHeights);

                int index = 0;
                foreach (var selectedFacades in f)
                {
                    facades[index].AddRange(selectedFacades);
                    index++;
                }
            }

            return new Design(AboveGroundFloors.Append(BelowGroundFloors), Footprints, Verticals, facades);
        }
    }
}
