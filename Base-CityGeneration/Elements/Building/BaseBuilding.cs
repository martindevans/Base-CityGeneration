using Base_CityGeneration.Elements.Building.Design;
using Base_CityGeneration.Elements.Building.Facades;
using Base_CityGeneration.Elements.Building.Internals.Floors;
using Base_CityGeneration.Elements.Building.Internals.VerticalFeatures;
using Base_CityGeneration.Styles;
using EpimetheusPlugins.Procedural;
using EpimetheusPlugins.Procedural.Utilities;
using EpimetheusPlugins.Scripts;
using Myre.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using SwizzleMyVectors;

namespace Base_CityGeneration.Elements.Building
{
    public abstract class BaseBuilding
        : ProceduralScript, IBuilding
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

        public IEnumerable<IBuildingFacade> Facades(int floor)
        {
            CheckSubdivided();
            return _facades.Where(f => f.BottomFloorIndex <= floor && f.TopFloorIndex >= floor);
        }
        #endregion

        public override void Subdivide(Prism bounds, ISubdivisionGeometry geometry, INamedDataCollection hierarchicalParameters)
        {
            //Select external building parameters
            var externals = SelectExternals();

            //Create things
            _floors = CreateFloors(SelectFloors(), Footprints(externals));
            _verticals = CreateVerticals(SelectVerticals(), _floors);
            _facades = CreateFacades(geometry, externals, hierarchicalParameters);

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

        private IReadOnlyDictionary<int, IFloor> CreateFloors(IEnumerable<FloorSelection> floors, Func<int, IReadOnlyList<Vector2>> footprintSelector)
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
                var footprint = footprintSelector(floor.Index);

                //Create node
                IFloor node = (IFloor)CreateChild(
                    new Prism(floor.Height, footprint),
                    Quaternion.Identity,
                    new Vector3(0, offset + floor.Height / 2f, 0),
                    floor.Script
                    );
                node.FloorIndex = floor.Index;
                node.FloorAltitude = offset;
                node.FloorHeight = floor.Height;

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

        protected abstract IEnumerable<VerticalSelection> SelectVerticals();

        private IReadOnlyCollection<IBuildingFacade> CreateFacades(ISubdivisionGeometry geometry, IEnumerable<Footprint> footprints, INamedDataCollection hierarchicalParameters)
        {
            //Accumulate results
            List<IBuildingFacade> results = new List<IBuildingFacade>();

            //Calculate external wall thickness
            var thickness = hierarchicalParameters.ExternalWallThickness(Random);
            var material = hierarchicalParameters.ExternalWallMaterial(Random);

            var footprintArr = footprints.OrderBy(a => a.BottomIndex).ToArray();
            for (int i = 0; i < footprintArr.Length; i++)
            {
                var footprint = footprintArr[i];
                var topIndex = (i == footprintArr.Length - 1) ? (_floors[_floors.Keys.Max()].FloorIndex) : (footprintArr[i + 1].BottomIndex - 1);

                //Sanity check that we have the correct number of facades
                if (footprint.Facades.Count != footprint.Shape.Count)
                    throw new InvalidOperationException(string.Format("Tried to created {0} facades for {1} walls", footprint.Facades.Count, footprint.Shape.Count));

                //Generate wall sections to fill in
                var footprintWallSections = footprint.Shape.Sections(thickness);

                //Split into corner sections and non corner sections
                var corners = footprintWallSections.Where(a => a.IsCorner).ToArray();
                var sections = footprintWallSections.Where(a => !a.IsCorner).ToArray();

                //Create the tiny bits of facade in the corners
                CreateCornerFacades(geometry, footprint, topIndex, corners, material);

                //Now iterate through sides and create facades
                CreatePrimaryFacades(footprint, sections, results);
            }

            return results;
        }

        private void CreatePrimaryFacades(Footprint footprint, Walls.Section[] sections, ICollection<IBuildingFacade> results)
        {
            for (int sideIndex = 0; sideIndex < footprint.Facades.Count; sideIndex++)
            {
                //Get start and end point of this wall
                var sideStart = footprint.Shape[sideIndex];
                var sideEnd = footprint.Shape[(sideIndex + 1) % footprint.Shape.Count];

                //find which section this side is for
                var sideSegment = new LineSegment2D(sideStart, sideEnd);
                var section = (from s in sections
                               let aD = Vector2.Distance(Geometry2D.ClosestPointOnLineSegment(sideSegment, s.ExternalLineSegment.Start), s.ExternalLineSegment.Start)
                               where aD < 0.1f
                               let bD = Vector2.Distance(Geometry2D.ClosestPointOnLineSegment(sideSegment, s.ExternalLineSegment.End), s.ExternalLineSegment.End)
                               where bD < 0.1f
                               let d = aD + bD
                               orderby d
                               select s).First();

                //There are multiple facades for any one wall section, iterate through them and create them
                foreach (var facade in footprint.Facades[sideIndex])
                {
                    //Sanity check that the facade does not underrun the valid range
                    //We can't sanity check overrun (easily) because that's based on the start of the *next* footprint
                    if (facade.Bottom < footprint.BottomIndex)
                        throw new InvalidOperationException(string.Format("Facade associated with wall at floor {0} attempted to place itself at floor {1}", footprint.BottomIndex, facade.Bottom));

                    var bot = _floors[facade.Bottom].FloorAltitude;
                    var top = _floors[facade.Top].FloorAltitude + _floors[facade.Top].FloorHeight;
                    var mid = (bot + top) * 0.5f;

                    var prism = new Prism(top - bot, section.A, section.B, section.C, section.D);

                    //Create a configurable facade in the space
                    var configurableNode = (ConfigurableFacade)CreateChild(prism, Quaternion.Identity, new Vector3(0, mid, 0), new ScriptReference(typeof(ConfigurableFacade)));
                    configurableNode.Section = section;

                    //Create the specified facade in the *same* space
                    var externalFacade = (BaseBuildingFacade)CreateChild(prism, Quaternion.Identity, new Vector3(0, mid, 0), facade.Script);
                    externalFacade.Facade = configurableNode;
                    externalFacade.Section = section;
                    externalFacade.BottomFloorIndex = facade.Bottom;
                    externalFacade.TopFloorIndex = facade.Top;
                    results.Add(externalFacade);

                    //Make sure the building facade subdivides before the configurable facade (this ensures it can configure the facade)
                    configurableNode.AddPrerequisite(externalFacade, true);

                    //Make sure floors subdivide before configurable facade (this ensures it too can configure the facade)
                    for (int i = externalFacade.BottomFloorIndex; i <= externalFacade.TopFloorIndex; i++)
                        configurableNode.AddPrerequisite(_floors[i], false);
                }
            }
        }

        private void CreateCornerFacades(ISubdivisionGeometry geometry, Footprint footprint, int topIndex, Walls.Section[] corners, string material)
        {
            //Calculate altitude of bottom and top of this facade
            var bot = _floors[footprint.BottomIndex].FloorAltitude;
            var top = _floors[topIndex].FloorAltitude + _floors[topIndex].FloorHeight;
            var mid = (bot + top) * 0.5f;

            //Fill in corner sections (solid)
            foreach (var corner in corners)
            {
                var prism = geometry.CreatePrism(material, new[] {
                    corner.A, corner.B, corner.C, corner.D
                }, top - bot).Translate(new Vector3(0, mid, 0));
                geometry.Union(prism);
            }
        }

        /// <summary>
        /// Select all the footprints which make up the shape of this building, as well as facades associated with them
        /// </summary>
        /// <returns></returns>
        protected abstract IEnumerable<Footprint> SelectExternals();

        #region helpers
        protected BuildingSideInfo[] GetNeighbourInfo(Prism bounds)
        {
            //todo: Get neighbour height data

            List<BuildingSideInfo> info = new List<BuildingSideInfo>();

            for (int i = 0; i < bounds.Footprint.Count; i++)
                info.Add(new BuildingSideInfo(bounds.Footprint[i], bounds.Footprint[(i + 1) % bounds.Footprint.Count], new BuildingSideInfo.NeighbourInfo[0]));

            return info.ToArray();
        }

        private void CheckSubdivided()
        {
            if (State == SubdivisionStates.NotSubdivided)
                throw new InvalidOperationException("Cannot query BaseBuilding before it is subdivided");
        }

        private static IReadOnlyList<Vector2> IntersectionOfFootprints(IFloor[] floors)
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

        private static Func<int, IReadOnlyList<Vector2>> Footprints(IEnumerable<Footprint> externals)
        {
            var footprints = externals.ToDictionary(a => a.BottomIndex, a => a);

            return (floor) => {
                //There's always a floor zero footprint
                if (floor == 0)
                    return footprints[0].Shape;

                //Search downwards from this floor for next footprint
                if (floor > 0)
                {
                    for (int i = floor; i >= 0; i--)
                    {
                        Footprint ft;
                        if (footprints.TryGetValue(i, out ft))
                            return ft.Shape;
                    }

                    throw new InvalidOperationException(string.Format("Failed to find a footprint below floor {0}", floor));
                }

                //Floor must be < 0
                //Search upwards from this floor for next footprint
                for (int i = 0; i <= 0; i++)
                {
                    Footprint ft;
                    if (footprints.TryGetValue(i, out ft))
                        return ft.Shape;
                }

                throw new InvalidOperationException(string.Format("Failed to find a footprint above floor {0}", floor));
            };
        }
        #endregion

        protected struct Footprint
        {
            public int BottomIndex { get; private set; }

            public IReadOnlyList<Vector2> Shape { get; private set; }

            public IReadOnlyList<IReadOnlyList<FacadeSelection>> Facades { get; private set; }

            public Footprint(int bottomIndex, IReadOnlyList<Vector2> shape, IReadOnlyList<IReadOnlyList<FacadeSelection>> facades)
                : this()
            {
                BottomIndex = bottomIndex;

                Shape = shape;
                Facades = facades;
            }
        }
    }
}
