using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Base_CityGeneration.Elements.Building.Design.Spec;
using Base_CityGeneration.Elements.Building.Design.Spec.FacadeConstraints;
using Base_CityGeneration.Elements.Building.Design.Spec.Markers;
using Base_CityGeneration.Elements.Building.Design.Spec.Markers.Algorithms;
using Base_CityGeneration.Elements.Building.Design.Spec.Ref;
using Base_CityGeneration.Utilities;
using Base_CityGeneration.Utilities.Numbers;
using EpimetheusPlugins.Scripts;
using HandyCollections.Extensions;
using JetBrains.Annotations;
using Myre.Collections;
using SharpYaml.Serialization;

namespace Base_CityGeneration.Elements.Building.Design
{
    public class BuildingDesigner
    {
        #region field and properties
        public IEnumerable<KeyValuePair<string, string>> Tags { get; private set; }
        public Guid Id { get; private set; }
        public string Description { get; private set; }

        private readonly VerticalElementSpec[] _verticalSelectors;
        public IEnumerable<VerticalElementSpec> VerticalSelectors
        {
            get
            {
                return _verticalSelectors;
            }
        }

        private readonly BaseFloorSelector[] _floorSelectors;
        public IEnumerable<BaseFloorSelector> FloorSelectors
        {
            get
            {
                return _floorSelectors;
            }
        }

        private readonly FacadeSpec[] _facadeSelectors;
        public IEnumerable<FacadeSpec> FacadeSelectors
        {
            get { return _facadeSelectors; }
        }

        public float MinHeight
        {
            get { return _floorSelectors.Sum(a => a.MinHeight); }
        }

        public float MaxHeight
        {
            get { return _floorSelectors.Sum(a => a.MaxHeight); }
        }
        #endregion

        #region constructor
        public BuildingDesigner(IEnumerable<KeyValuePair<string, string>> tags, Guid id, string description, BaseFloorSelector[] floorSelectors, VerticalElementSpec[] verticalSelectors, FacadeSpec[] facadeSelectors)
        {
            Tags = tags;
            Id = id;
            Description = description;

            _floorSelectors = floorSelectors;
            _verticalSelectors = verticalSelectors;
            _facadeSelectors = facadeSelectors;
        }
        #endregion

        /// <summary>
        /// Evaluate this building design spec to create a set of building internals (floors and vertical elements)
        /// </summary>
        /// <param name="random"></param>
        /// <param name="metadata"></param>
        /// <param name="finder"></param>
        /// <returns></returns>
        public Internals Internals(Func<double> random, INamedDataCollection metadata, Func<IEnumerable<KeyValuePair<string, string>>, ScriptReference> finder)
        {
            var ground = _floorSelectors.OfType<GroundMarker>().Single();
            var aboveGround = _floorSelectors.TakeWhile(a => !(a is GroundMarker)).Append(ground).ToArray();
            var belowGround = _floorSelectors.SkipWhile(a => !(a is GroundMarker)).Skip(1).ToArray();

            List<FootprintSelection> footprints = new List<FootprintSelection>();

            //Select above ground floors, then assign indices
            var above = SelectFloors(random, metadata, finder, aboveGround, ground, aboveGround: true);
            int index = 0;
            float compoundHeight = 0;
            foreach (var run in above.Reverse())
            {
                if (run.Selection.Count > 0)
                {
                    footprints.Add(new FootprintSelection(run.Marker, index));
                    foreach (var floor in run.Selection.Reverse())
                    {
                        floor.Index = index++;
                        floor.CompoundHeight = compoundHeight;

                        compoundHeight += floor.Height;
                    }
                }
            }

            //Select below ground floors, then assign indices
            var below = SelectFloors(random, metadata, finder, belowGround, ground, aboveGround: false);
            index = 0;
            compoundHeight = 0;
            foreach (var run in below)
            {
                foreach (var floor in run.Selection)
                {
                    floor.Index = --index;
                    floor.CompoundHeight = compoundHeight;

                    compoundHeight -= floor.Height;
                }
                footprints.Add(new FootprintSelection(run.Marker, index));
            }

            //Create result object (with floors)
            var internals = new Internals(this, above.Select(a => a.Selection.ToArray()).ToArray(), below.Select(a => a.Selection.ToArray()).ToArray(), footprints.ToArray());

            //Select vertical elements for floors and add to result
            internals.Verticals = SelectVerticals(random, finder, _verticalSelectors, internals.Floors).ToArray();

            //return result
            return internals;
        }

        #region selection
        /// <summary>
        /// Select multiple runs of floors, ordered top down. Runs are split when the footprint of the floor changes (i.e. on Footprint Markers)
        /// </summary>
        /// <param name="random"></param>
        /// <param name="metadata"></param>
        /// <param name="finder"></param>
        /// <param name="selectors"></param>
        /// <param name="groundMarker"></param>
        /// <param name="aboveGround"></param>
        /// <returns></returns>
        private static IEnumerable<FloorRun> SelectFloors(Func<double> random, INamedDataCollection metadata, Func<IEnumerable<KeyValuePair<string, string>>, ScriptReference> finder, IEnumerable<BaseFloorSelector> selectors, BaseMarker groundMarker, bool aboveGround)
        {
            List<FloorRun> runs = new List<FloorRun>();

            BaseMarker previousMarker = groundMarker;
            List<FloorSelection> floors = new List<FloorSelection>();
            //Selectors, ordered top to bottom
            foreach (var selector in selectors)
            {
                //Runs, ordered top to bottom
                var r = selector.Select(random, metadata, finder);

                foreach (var floorRun in r)
                {
                    floors.AddRange(floorRun.Selection);

                    if (floorRun.Marker != null)
                    {
                        if (aboveGround)
                        {
                            runs.Add(new FloorRun(floors.ToArray(), floorRun.Marker));
                            floors.Clear();
                        }
                        else
                        {
                            runs.Add(new FloorRun(floors.ToArray(), previousMarker));
                            previousMarker = floorRun.Marker;
                            floors.Clear();
                        }
                    }
                }
            }

            //Sanity check
            if (aboveGround && floors.Count > 0)
                throw new InvalidOperationException("Leftover floors above ground - no ground marker?");

            //Final below ground run (doesn't have a footprint at the bottom, because udnerground floors have footprints above not below)
            if (!aboveGround && floors.Count > 0)
                runs.Add(new FloorRun(floors.ToArray(), previousMarker));

            return runs;
        }

        private static IEnumerable<VerticalSelection> SelectVerticals(Func<double> random, Func<IEnumerable<KeyValuePair<string, string>>, ScriptReference> finder, IEnumerable<VerticalElementSpec> verticalSelectors, IEnumerable<FloorSelection> floors)
        {
            List<VerticalSelection> verticals = new List<VerticalSelection>();

            var fArr = floors.ToArray();

            foreach (var selector in verticalSelectors)
                verticals.AddRange(selector.Select(random, finder, fArr));

            return verticals;
        }

        internal IEnumerable<FacadeSelection> SelectFacadesForWall(Func<double> random, Func<IEnumerable<KeyValuePair<string, string>>, ScriptReference> finder, IEnumerable<FloorSelection> floorRun, BuildingSideInfo[] neighbours, Vector2 ftStart, Vector2 ftEnd)
        {
            List<FacadeSelection> result = new List<FacadeSelection>();

            //Runs which we have no yet selected a facade for. Starts with just the input runs
            Stack<List<FloorSelection>> runs = new Stack<List<FloorSelection>>(new[] {
                floorRun.ToList()
            });

            //Keep applying specs to runs
            while (runs.Count > 0)
            {
                //Choose a run to process
                var run = runs.Pop();

                //Process it, adding a load of facades to the output, as well as producing a new set of runs
                var produced = SelectFacadesForRun(random, finder, run, neighbours, ftStart, ftEnd, result);

                //Add all the new runs to the stack
                foreach (var item in produced)
                    runs.Push(item);
            }

            return result;
        }

        private IEnumerable<List<FloorSelection>> SelectFacadesForRun(Func<double> random, Func<IEnumerable<KeyValuePair<string, string>>, ScriptReference> finder, List<FloorSelection> floors, BuildingSideInfo[] neighbours, Vector2 ftStart, Vector2 ftEnd, ICollection<FacadeSelection> results)
        {
            //working from the first selector to the last, try to find a facade for each floor
            foreach (var spec in FacadeSelectors)
            {
                //Find scripts for this spec
                KeyValuePair<string, string>[] selectedTags;
                ScriptReference script = spec.Tags.SelectScript(random, finder, out selectedTags);

                //Skip specs which cannot find a script
                if (script == null)
                    continue;

                //Remove floors from this run which do not pass the constraints of this facade spec
                //This produces a new set of runs (e.g. if we removed the middle floor of the run then we produce 2 runs - one above and one below the removed floor)
                var constrainedRuns = ConstrainRun(floors, spec.Constraints, neighbours, ftStart, ftEnd);

                bool topAny = false;
                foreach (var run in constrainedRuns)
                {
                    //Find top and bottom floors which match this spec
                    var facades = spec.Top.MatchFrom(run, spec.Bottom, spec.Bottom.Match(run, null));

                    foreach (var facade in facades)
                    {
                        topAny = true;

                        //Create a face over this range of floors
                        var min = Math.Min(facade.Key.Index, facade.Value.Index);
                        var max = Math.Max(facade.Key.Index, facade.Value.Index);
                        results.Add(new FacadeSelection(script, min, max));

                        //Remove these floors from the run
                        floors.RemoveAll(a => a.Index >= min && a.Index <= max);
                    }
                }

                if (!topAny)
                    continue;

                return SplitIntoContinuousRuns(floors);
            }

            throw new DesignFailedException(
                string.Format("Could not find an applicable facade spec for floor run [{0}]", string.Join(",", floors.Select(a => string.Format("{0}({1})", a.Index, a.Id))))
            );
        }

        private static IEnumerable<List<FloorSelection>> SplitIntoContinuousRuns(List<FloorSelection> run)
        {
            //Make sure run is in correct order (top down)
            run.Sort((a, b) => b.Index.CompareTo(a.Index));

            //Split the run up into multiple new runs
            List<FloorSelection> newRun = new List<FloorSelection>();
            for (int i = 0; i < run.Count; i++)
            {
                //If this is a new run, or the previous floors is continuous with this one we're ok
                if (newRun.Count == 0 || newRun[newRun.Count - 1].Index == run[i].Index + 1)
                    newRun.Add(run[i]);
                else
                {
                    //This floor is *not* continuous with previous floor, start a new run
                    yield return newRun;
                    newRun = new List<FloorSelection> { run[i] };
                }
            }

            //Make sure to return the last result (if it's not empty)
            if (newRun.Count > 0)
                yield return newRun;
        }

        /// <summary>
        /// Given a run of floors apply constraints to eliminate floors and generate sub runs
        /// </summary>
        /// <returns></returns>
        private IEnumerable<List<FloorSelection>> ConstrainRun(IEnumerable<FloorSelection> run, IEnumerable<BaseFacadeConstraint> constraints, BuildingSideInfo[] neighbours, Vector2 ftStart, Vector2 ftEnd)
        {
            //Floors which pass all constraints
            List<FloorSelection> passed = (
                from floor in run
                where constraints.All(c => c.Check(floor, neighbours, ftStart, ftEnd, floor.CompoundHeight, floor.Height))
                select floor
            ).ToList();

            return SplitIntoContinuousRuns(passed);
        }
        #endregion

        #region serialization
        private static Serializer CreateSerializer()
        {
            var serializer = new Serializer(new SerializerSettings
            {
                EmitTags = true,
            });

            //Root type
            serializer.Settings.RegisterTagMapping("Building", typeof(Container));

            //Markers
            serializer.Settings.RegisterTagMapping("Ground", typeof(GroundMarker.Container));
            serializer.Settings.RegisterTagMapping("Footprint", typeof(FootprintMarker.Container));
            serializer.Settings.RegisterTagMapping("Shrink", typeof(Shrink.Container));
            serializer.Settings.RegisterTagMapping("Clip", typeof(Clip.Container));
            serializer.Settings.RegisterTagMapping("Twist", typeof(Twist.Container));

            //Floor element types
            serializer.Settings.RegisterTagMapping("Floor", typeof(FloorSpec.Container));
            serializer.Settings.RegisterTagMapping("Range", typeof(FloorRangeSpec.Container));
            serializer.Settings.RegisterTagMapping("Include", typeof(FloorRangeIncludeSpec.Container));
            serializer.Settings.RegisterTagMapping("Repeat", typeof(RepeatSpec.Container));

            //Facade types
            serializer.Settings.RegisterTagMapping("Access", typeof(AccessConstraint.Container));
            serializer.Settings.RegisterTagMapping("Clearance", typeof(ClearanceConstraint.Container));

            //Utility types
            serializer.Settings.RegisterTagMapping("NormalValue", typeof(NormallyDistributedValue.Container));
            serializer.Settings.RegisterTagMapping("UniformValue", typeof(UniformlyDistributedValue.Container));

            //Ref types
            serializer.Settings.RegisterTagMapping("Num", typeof(NumRef.Container));
            serializer.Settings.RegisterTagMapping("Tagged", typeof(TaggedRef.Container));
            serializer.Settings.RegisterTagMapping("Id", typeof(IdRef.Container));
            serializer.Settings.RegisterTagMapping("RegexId", typeof(RegexIdRef.Container));

            return serializer;
        }

        public static BuildingDesigner Deserialize(TextReader reader)
        {
            var s = CreateSerializer();

            return s.Deserialize<Container>(reader).Unwrap();
        }

        internal class Container
        {
            // ReSharper disable once CollectionNeverUpdated.Global
            public Dictionary<string, string> Tags { get; set; }
            public string Id { get; set; }
            public string Description { get; set; }

            public List<object> Aliases { get; set; }

            public VerticalElementSpec.Container[] Verticals { get; [UsedImplicitly]set; }
            public FacadeSpec.Container[] Facades { get; [UsedImplicitly]set; }
            public ISelectorContainer[] Floors { get; [UsedImplicitly]set; }

            public BuildingDesigner Unwrap()
            {
                return new BuildingDesigner(
                    Tags,
                    Guid.Parse(Id ?? Guid.NewGuid().ToString()),
                    Description,
                    Floors.Select(a => a.Unwrap()).ToArray(),
                    (Verticals ?? new VerticalElementSpec.Container[0]).Select(a => a.Unwrap()).ToArray(),
                    (Facades ?? new FacadeSpec.Container[0]).Select(a => a.Unwrap()).ToArray()
                );
            }
        }
        #endregion
    }
}
