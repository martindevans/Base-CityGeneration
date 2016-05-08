using Base_CityGeneration.Elements.Building.Design;
using Base_CityGeneration.Elements.Building.Facades;
using Base_CityGeneration.Elements.Building.Internals.Floors;
using Base_CityGeneration.Styles;
using EpimetheusPlugins.Procedural;
using EpimetheusPlugins.Scripts;
using Myre.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Numerics;
using Base_CityGeneration.Elements.Blocks;
using Base_CityGeneration.Elements.Building.Internals.VerticalFeatures;
using Base_CityGeneration.Elements.Roads;
using Base_CityGeneration.Geometry.Walls;
using SwizzleMyVectors.Geometry;

namespace Base_CityGeneration.Elements.Building
{
    [ContractClass(typeof(BaseBuildingContracts))]
    public abstract class BaseBuilding
        : ProceduralScript, IBuilding
    {
        public float GroundHeight { get; set; }

        public IReadOnlyList<NeighbourInfo> Neighbours { get; set; }

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
        private IReadOnlyList<VerticalSelection> _verticals;

        /// <summary>
        /// Get all vertical features which overlap the given floor range
        /// </summary>
        /// <param name="lowest">Bottom floor of returned verticals must be less than or equal to this</param>
        /// <param name="highest">Top floor of returned verticals must be greater than or equal to this</param>
        /// <returns></returns>
        public IEnumerable<VerticalSelection> Verticals(int lowest, int highest)
        {
            CheckSubdivided();
            return _verticals.Where(a => a.Bottom <= lowest && a.Top >= highest);
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
            _verticals = SelectVerticals().ToArray();
            _facades = CreateFacades(geometry, externals, hierarchicalParameters);

            //Setup relationship between floors (floor PrerequisiteOf floor-further-from-ground)
            foreach (var keyValuePair in _floors)
            {
                //Ground floor has no prerequisites
                if (keyValuePair.Key == 0)
                    continue;

                //Work out the index of the prerequisite
                var prereqIndex = keyValuePair.Key < 0 ? keyValuePair.Key + 1 : keyValuePair.Key - 1;

                //Try to find it (this *shouldn't* ever fail, if we have a continuous run of floors)
                IFloor prereq;
                if (_floors.TryGetValue(prereqIndex, out prereq))
                    keyValuePair.Value.AddPrerequisite(prereq);
            }
        }

        private IReadOnlyDictionary<int, IFloor> CreateFloors(IEnumerable<FloorSelection> floors, Func<int, IReadOnlyList<Vector2>> footprintSelector)
        {
            Contract.Requires(floors != null);
            Contract.Requires(footprintSelector != null);

            //Sanity check selection does not have two floors in the same place
            if (floors.GroupBy(a => a.Index).Any(g => g.Count() > 1))
                throw new InvalidOperationException("Attempted to create two floors with the same index");

            //Count up the number of floors below ground
            _belowGroundFloors = floors.Count(a => a.Index < 0);

            //Collection of results (floor index -> floor node)
            var result = new Dictionary<int, IFloor>();

            //calculate offset to lowest basement
            //todo: do I need to add/subtract GroundHeight to this?
            var offset = -floors.Where(a => a.Index < 0).Sum(a => a.Height);

            //Materialize selection into child nodes
            foreach (var floor in floors.OrderBy(a => a.Index))
            {
                //Get the footprint for this floor
                var footprint = footprintSelector(floor.Index);

                //Create node
                var node = (IFloor)CreateChild(
                    new Prism(floor.Height, footprint),
                    Quaternion.Identity,
                    new Vector3(0, offset + floor.Height / 2f, 0),
                    floor.Script
                );
                node.FloorIndex = floor.Index;
                node.FloorAltitude = offset;
                CreatedFloor(node);

                //Move offset up for next floor
                offset += floor.Height;

                //Add to result collection
                result.Add(node.FloorIndex, node);
            }

            CreatedFloors(result);
            return result;
        }

        /// <summary>
        /// Called immediately after the given floor has been created
        /// </summary>
        /// <remarks>Other arbitrary floors may not yet have been constructed!</remarks>
        /// <param name="node"></param>
        protected virtual void CreatedFloor(IFloor node)
        {
        }

        /// <summary>
        /// Called immediately after all floors have been created
        /// </summary>
        /// <param name="floors"></param>
        protected virtual void CreatedFloors(IReadOnlyDictionary<int, IFloor> floors)
        {
        }

        protected abstract IEnumerable<FloorSelection> SelectFloors();

        protected abstract IEnumerable<VerticalSelection> SelectVerticals();

        private IReadOnlyCollection<IBuildingFacade> CreateFacades(ISubdivisionGeometry geometry, IEnumerable<Footprint> footprints, INamedDataCollection hierarchicalParameters)
        {
            Contract.Requires(geometry != null);
            Contract.Requires(footprints != null);
            Contract.Requires(hierarchicalParameters != null);

            //Accumulate results
            var results = new List<IBuildingFacade>();

            //Calculate external wall thickness
            var thickness = hierarchicalParameters.ExternalWallThickness(Random);
            var material = hierarchicalParameters.ExternalWallMaterial(Random);

            var footprintArr = footprints.OrderBy(a => a.BottomIndex).ToArray();
            for (var i = 0; i < footprintArr.Length; i++)
            {
                var footprint = footprintArr[i];
                var topIndex = (i == footprintArr.Length - 1) ? (_floors[_floors.Keys.Max()].FloorIndex) : (footprintArr[i + 1].BottomIndex - 1);

                //Sanity check that we have the correct number of facades
                if (footprint.Facades.Count != footprint.Shape.Count)
                    throw new InvalidOperationException(string.Format("Tried to created {0} facades for {1} walls", footprint.Facades.Count, footprint.Shape.Count));

                //Generate wall sections to fill in
                Vector2[] inner;
                IReadOnlyList<IReadOnlyList<Vector2>> corners;
                var sections = footprint.Shape.Sections(thickness, out inner, out corners);

                //Create the tiny bits of facade in the corners
                CreateCornerFacades(geometry, footprint, topIndex, corners, material);

                //Now iterate through sides and create facades
                CreatePrimaryFacades(footprint, sections, results);
            }

            return results;
        }

        private void CreatePrimaryFacades(Footprint footprint, IEnumerable<Section> sections, ICollection<IBuildingFacade> results)
        {
            for (var sideIndex = 0; sideIndex < footprint.Facades.Count; sideIndex++)
            {
                //Get start and end point of this wall
                var sideStart = footprint.Shape[sideIndex];
                var sideEnd = footprint.Shape[(sideIndex + 1) % footprint.Shape.Count];

                //find which section this side is for
                var sideSegment = new LineSegment2(sideStart, sideEnd).Line;
                var maybeSection = (from s in sections
                               let aP = sideSegment.ClosestPointDistanceAlongLine(s.ExternalLineSegment.Start) * sideSegment.Direction + sideSegment.Position
                               let aD = Vector2.Distance(aP, s.ExternalLineSegment.Start)
                               where aD < 0.1f
                               let bP = sideSegment.ClosestPointDistanceAlongLine(s.ExternalLineSegment.End) * sideSegment.Direction + sideSegment.Position
                               let bD = Vector2.Distance(bP, s.ExternalLineSegment.End)
                               where bD < 0.1f
                               let d = aD + bD
                               orderby d
                               select s).Cast<Section?>().FirstOrDefault();

                //Failed to find a section, this can happen when wall segments are so small the two corner segments either end completely cover the actual wall
                if (!maybeSection.HasValue)
                    continue;
                var section = maybeSection.Value;

                //There are multiple facades for any one wall section, iterate through them and create them
                foreach (var facade in footprint.Facades[sideIndex])
                {
                    //Sanity check that the facade does not underrun the valid range
                    //We can't sanity check overrun (easily) because that's based on the start of the *next* footprint
                    if (facade.Bottom < footprint.BottomIndex)
                        throw new InvalidOperationException(string.Format("Facade associated with wall at floor {0} attempted to place itself at floor {1}", footprint.BottomIndex, facade.Bottom));

                    var bot = _floors[facade.Bottom].FloorAltitude;
                    var top = _floors[facade.Top].FloorAltitude + _floors[facade.Top].Bounds.Height;
                    var mid = (bot + top) * 0.5f;

                    var prism = new Prism(top - bot, section.Inner1, section.Inner2, section.Outer1, section.Outer2);

                    //Create a configurable facade in the space
                    var configurableNode = (ConfigurableFacade)CreateChild(prism, Quaternion.Identity, new Vector3(0, mid, 0), new ScriptReference(typeof(ConfigurableFacade)));
                    configurableNode.Section = section;

                    //Create the specified facade in the *same* space
                    //This facade is just a proxy which passes all it's stamps to the configurable facade (created above)
                    var externalFacade = (BaseBuildingFacade)CreateChild(prism, Quaternion.Identity, new Vector3(0, mid, 0), facade.Script);
                    externalFacade.Facade = configurableNode;
                    externalFacade.Section = section;
                    externalFacade.BottomFloorIndex = facade.Bottom;
                    externalFacade.TopFloorIndex = facade.Top;
                    results.Add(externalFacade);

                    //Make sure the building facade subdivides before the configurable facade (this ensures it can configure the facade)
                    configurableNode.AddPrerequisite(externalFacade, true);

                    //Make sure floors subdivide before configurable facade (this ensures it too can configure the facade)
                    for (var i = externalFacade.BottomFloorIndex; i <= externalFacade.TopFloorIndex; i++)
                        configurableNode.AddPrerequisite(_floors[i], false);

                    //Make sure the building facade subdivides before the floor (this ensures the floor can see the effects of the facade)
                    for (var i = externalFacade.BottomFloorIndex; i <= externalFacade.TopFloorIndex; i++)
                        _floors[i].AddPrerequisite(externalFacade, true);

                }
            }
        }

        private void CreateCornerFacades(ISubdivisionGeometry geometry, Footprint footprint, int topIndex, IReadOnlyList<IReadOnlyList<Vector2>> corners, string material)
        {
            Contract.Requires(geometry != null);
            Contract.Requires(corners != null);
            Contract.Requires(_floors != null);

            //Calculate altitude of bottom and top of this facade
            var bot = _floors[footprint.BottomIndex].FloorAltitude;
            var top = _floors[topIndex].FloorAltitude + _floors[topIndex].Bounds.Height;
            var mid = (bot + top) * 0.5f;

            //Fill in corner sections (solid)
            foreach (var corner in corners)
            {

                try
                {
                    geometry.Union(geometry
                        .CreatePrism(material, corner, top - bot)
                        .Translate(new Vector3(0, mid, 0))
                    );
                }
                catch (ArgumentException)
                {
                    //Suppress the argument exception, why?
                    //If we try to create a degenerate prism (negative height, only 2 points) we get an arg exception
                    //Corner sections can be very slim, sometimes *so slim* that the four points merge together
                    //In this case we don't care, the section is so small we'll just skip it!
                }
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
            Contract.Ensures(Contract.Result<BuildingSideInfo[]>() != null);
            Contract.Ensures(Contract.Result<BuildingSideInfo[]>().Length == bounds.Footprint.Count);

            var sides = new BuildingSideInfo[bounds.Footprint.Count];

            for (var i = 0; i < bounds.Footprint.Count; i++)
            {
                //Start and end point of this segment
                var a = bounds.Footprint[i];
                var b = bounds.Footprint[(i + 1) % bounds.Footprint.Count];
                var seg = new LineSegment2(a, b).Transform(WorldTransformation);
                var line = seg.Line;

                //Neighbours which are for this segment
                var ns = from n in (Neighbours ?? new NeighbourInfo[0])
                         where (n.Segment.Line.Parallelism(line) == Parallelism.Collinear)
                         let result = ExtractDataFromUnknownNode(n.Neighbour)
                         select new BuildingSideInfo.NeighbourInfo(n.Start, n.End, result.Key, result.Value);

                //Save the results
                sides[i] = new BuildingSideInfo(a, b, ns.ToArray());
            }

            return sides;
        }

        private KeyValuePair<float, BuildingSideInfo.NeighbourInfo.Resource[]> ExtractDataFromUnknownNode(ISubdivisionContext neighbour)
        {
            #region fields
            //Wouldn't it be great if we were using C#7 and we could use a match statement on type...
            //...Instead we're going to hide the dirty details in here
            IBuildingContainer ibc;
            IRoad ir;
            #endregion

            if ((ibc = neighbour as IBuildingContainer) != null)
            {
                return new KeyValuePair<float, BuildingSideInfo.NeighbourInfo.Resource[]>(ibc.Height, new BuildingSideInfo.NeighbourInfo.Resource[0]);
            }
            else if ((ir = neighbour as IRoad) != null)
            {
                return new KeyValuePair<float, BuildingSideInfo.NeighbourInfo.Resource[]>(0, new BuildingSideInfo.NeighbourInfo.Resource[] {
                    new BuildingSideInfo.NeighbourInfo.Resource(0, 0, "road")
                });
            }

            return new KeyValuePair<float, BuildingSideInfo.NeighbourInfo.Resource[]>(0, new BuildingSideInfo.NeighbourInfo.Resource[0]);
        }

        private void CheckSubdivided()
        {
            if (State == SubdivisionStates.NotSubdivided)
                throw new InvalidOperationException("Cannot query BaseBuilding before it is subdivided");
        }

        private static Func<int, IReadOnlyList<Vector2>> Footprints(IEnumerable<Footprint> externals)
        {
            Contract.Requires(externals != null);
            Contract.Ensures(Contract.Result<Func<int, IReadOnlyList<Vector2>>>() != null);

            var footprints = externals.ToDictionary(a => a.BottomIndex, a => a);

            if (!footprints.ContainsKey(0))
                throw new ArgumentException("Externals must contain a footprint for floor zero", "externals");

            return floor => {
                //There's always a floor zero footprint
                if (floor == 0)
                    return footprints[0].Shape;

                //Search downwards from this floor for next footprint
                if (floor > 0)
                {
                    for (var i = floor; i >= 0; i--)
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

        #region IVerticalFeatureContainer implementation
        private readonly List<KeyValuePair<VerticalSelection, IVerticalFeature>> _verticalNodes = new List<KeyValuePair<VerticalSelection, IVerticalFeature>>();

        void IVerticalFeatureContainer.Add(VerticalSelection selection, IVerticalFeature feature)
        {
            //Ensure that all the floors overlapping this vertical are not subdivided
            for (int i = selection.Bottom; i < selection.Top; i++)
                if (Floor(i).State != SubdivisionStates.NotSubdivided)
                    throw new InvalidOperationException("Cannot add vertical element overlapping a floor which is already subdivided");

            //This element is valid, store it
            _verticalNodes.Add(new KeyValuePair<VerticalSelection, IVerticalFeature>(selection, feature));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="floorIndex"></param>
        /// <param name="checkSubdivided"></param>
        /// <returns></returns>
        IEnumerable<KeyValuePair<VerticalSelection, IVerticalFeature>> IVerticalFeatureContainer.Overlapping(int floorIndex, bool checkSubdivided)
        {
            //Check that all floors closer to zero are already subdivided
            foreach (var index in floorIndex >= 0 ? Enumerable.Range(0, floorIndex) : Enumerable.Range(floorIndex, -floorIndex))
            {
                var floor = Floor(index);
                if (checkSubdivided && floor.State != SubdivisionStates.Subdivided)
                    throw new InvalidOperationException(string.Format("Cannot get vertical elements for floor {0} - floor {1} is not yet subdivided", floor, index));
            }

            return _verticalNodes.Where(a => a.Key.Bottom <= floorIndex && a.Key.Top >= floorIndex);
        }
        #endregion

        /// <summary>
        /// Information about a footprint of the building, building footprint can change on arbitrary floors of the buildings
        /// </summary>
        public struct Footprint
        {
            /// <summary>
            /// Footprint covers all floors up to the next footprint from this floor
            /// </summary>
            public int BottomIndex { get; private set; }

            /// <summary>
            /// The shape of this footprint
            /// </summary>
            public IReadOnlyList<Vector2> Shape { get; private set; }

            /// <summary>
            /// Facades to place around this footprint. Outer list is indexed by side, inner list is for multiple facades covering a single side (over different floors)
            /// </summary>
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

    [ContractClassFor(typeof(BaseBuilding))]
    internal abstract class BaseBuildingContracts
        : BaseBuilding
    {
        protected override IEnumerable<FloorSelection> SelectFloors()
        {
            Contract.Ensures(Contract.Result<IEnumerable<FloorSelection>>() != null);

            return default(IEnumerable<FloorSelection>);
        }

        protected override IEnumerable<VerticalSelection> SelectVerticals()
        {
            Contract.Ensures(Contract.Result<IEnumerable<VerticalSelection>>() != null);

            return default(IEnumerable<VerticalSelection>);
        }

        protected override IEnumerable<Footprint> SelectExternals()
        {
            Contract.Ensures(Contract.Result<IEnumerable<Footprint>>() != null);

            return default(IEnumerable<Footprint>);
        }

        public override bool Accept(Prism bounds, INamedDataProvider parameters)
        {
            return default(bool);
        }
    }
}
