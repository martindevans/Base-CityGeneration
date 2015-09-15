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
        public BuildingDesigner(BaseFloorSelector[] floorSelectors, VerticalElementSpec[] verticalSelectors, FacadeSpec[] facadeSelectors)
        {
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
        public Internals Internals(Func<double> random, INamedDataCollection metadata, Func<string[], ScriptReference> finder)
        {
            var ground = _floorSelectors.OfType<GroundMarker>().Single();
            var aboveGround = _floorSelectors.TakeWhile(a => !(a is GroundMarker)).Append(ground).ToArray();
            var belowGround = _floorSelectors.SkipWhile(a => !(a is GroundMarker)).Skip(1).ToArray();

            List<FootprintSelection> footprints = new List<FootprintSelection>();

            //Select above ground floors, then assign indices
            var above = SelectFloors(random, metadata, finder, aboveGround, null);
            int index = 0;
            foreach (var run in above.Reverse())
            {
                footprints.Add(new FootprintSelection(run.Marker, index));
                foreach (var floor in run.Selection.Reverse())
                    floor.Index = index++;
            }

            //Select below ground floors, then assign indices
            var below = SelectFloors(random, metadata, finder, belowGround,ground);
            index = 0;
            foreach (var run in below)
            {
                foreach (var floor in run.Selection)
                    floor.Index = --index;
                footprints.Add(new FootprintSelection(run.Marker, index));
            }

            //Create result object (with floors)
            var internals = new Internals(this, above.Select(a => a.Selection).ToArray(), below.Select(a => a.Selection).ToArray(), footprints.ToArray());

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
        /// <param name="defaultMarker"></param>
        /// <returns></returns>
        private static FloorRun[] SelectFloors(Func<double> random, INamedDataCollection metadata, Func<string[], ScriptReference> finder, IEnumerable<BaseFloorSelector> selectors, BaseMarker defaultMarker)
        {
            List<FloorRun> floors = new List<FloorRun>();

            BaseMarker currentMarker = defaultMarker;
            List<FloorSelection> currentRun = new List<FloorSelection>();
            foreach (var selector in selectors)
            {
                var marker = selector as BaseMarker;
                if (marker != null)
                {
                    currentMarker = marker;
                    if (currentRun.Count > 0)
                    {
                        floors.Add(new FloorRun(currentRun.ToArray(), currentMarker));
                        currentRun.Clear();
                    }
                }
                else
                    currentRun.AddRange(selector.Select(random, metadata, finder));
            }
            if (currentRun.Count > 0)
                floors.Add(new FloorRun(currentRun.ToArray(), currentMarker));

            if (floors.Any(a => a.Marker == null))
                throw new InvalidOperationException("Tried to create a run of floors with a null marker");

            return floors.ToArray();
        }

        private class FloorRun
        {
            public readonly FloorSelection[] Selection;
            public readonly BaseMarker Marker;

            public FloorRun(FloorSelection[] floors, BaseMarker marker)
            {
                Selection = floors;
                Marker = marker;
            }
        }

        private static IEnumerable<VerticalSelection> SelectVerticals(Func<double> random, Func<string[], ScriptReference> finder, IEnumerable<VerticalElementSpec> verticalSelectors, IEnumerable<FloorSelection> floors)
        {
            List<VerticalSelection> verticals = new List<VerticalSelection>();

            var fArr = floors.ToArray();

            foreach (var selector in verticalSelectors)
                verticals.AddRange(selector.Select(random, finder, fArr));

            return verticals;
        }

        internal IEnumerable<FacadeSelection> SelectFacadesForWall(Func<double> random, Func<string[], ScriptReference> finder, IEnumerable<FloorSelection> floorRun, BuildingSideInfo[] neighbours, Vector2 ftStart, Vector2 ftEnd)
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
                var produced = SelectFacadesForRun(random, finder, FacadeSelectors, run, result);

                //Add all the new runs to the stack
                foreach (var item in produced)
                    runs.Push(item);
            }

            return result;

            ////Select floors which are above the required height
            ////We keep a list of uninterrupted runs of floors, and then match facades on each run
            ////to start with there is just one run
            //Stack<List<FloorSelection>> runs = new Stack<List<FloorSelection>>(new[] {
            //    (from floor in aboveGroundFloors
            //     let below = aboveGroundFloors.Where(f => f.Index < floor.Index).Select(f => f.Height).Sum()
            //     where below >= startHeight
            //     select floor).ToList()
            //}.Where(r => r.Count > 0));

            ////This entire wall is obscured, no facades
            //if (runs.Count == 0)
            //    return new FacadeSelection[0];

            ////Set of selected facades
            //List<FacadeSelection> facades = new List<FacadeSelection>();

            ////Keep applying specs to runs
            //while (runs.Count > 0)
            //{
            //    //Choose a run to process
            //    var run = runs.Pop();

            //    //Process it, adding a load of facades to the output, as well as producing a new set of runs
            //    var produced = SelectFacadesForRun(random, finder, specs, run, facades);

            //    //Add all the new runs to the stack
            //    foreach (var item in produced)
            //        runs.Push(item);
            //}

            //return facades;
        }

        private static IEnumerable<List<FloorSelection>> SelectFacadesForRun(Func<double> random, Func<string[], ScriptReference> finder, IEnumerable<FacadeSpec> specs, List<FloorSelection> run, ICollection<FacadeSelection> results)
        {
            //working from the first selector to the last, try to find a facade for each floor
            foreach (var spec in specs)
            {
                //Find scripts for this spec
                string[] selectedTags;
                ScriptReference script = spec.Tags.SelectScript(random, finder, out selectedTags);
                if (script == null)
                    continue;

                //Find top and bottom floors which match this spec
                var bot = spec.Bottom.Match(run, null);
                var top = spec.Top.MatchFrom(run, spec.Bottom, bot);

                bool topAny = false;
                foreach (var facade in top)
                {
                    topAny = true;

                    //Create a face over this range of floors
                    var min = Math.Min(facade.Key.Index, facade.Value.Index);
                    var max = Math.Max(facade.Key.Index, facade.Value.Index);
                    results.Add(new FacadeSelection(script, min, max));

                    //Remove these floors from the run
                    run.RemoveAll(a => a.Index >= min && a.Index <= max);
                }

                if (!topAny)
                    continue;
                
                List<List<FloorSelection>> runs = new List<List<FloorSelection>>();

                //Split the run up into multiple new runs
                List<FloorSelection> newRun = new List<FloorSelection>();
                for (int i = 0; i < run.Count; i++)
                {
                    if (newRun.Count == 0 || newRun[newRun.Count - 1].Index == run[i].Index + 1)
                        newRun.Add(run[i]);
                    else
                    {
                        runs.Add(newRun);
                        newRun = new List<FloorSelection> { run[i] };
                    }
                }
                if (newRun.Count > 0)
                    runs.Add(newRun);

                return runs;
            }

            throw new DesignFailedException(
                string.Format("Could not find an applicable facade spec for floor run [{0}]", string.Join(",", run.Select(a => string.Format("{0}({1})", a.Index, a.Id))))
            );
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

            //Floor element types
            serializer.Settings.RegisterTagMapping("Floor", typeof(FloorSpec.Container));
            serializer.Settings.RegisterTagMapping("Range", typeof(FloorRangeSpec.Container));
            serializer.Settings.RegisterTagMapping("Include", typeof(FloorRangeIncludeSpec.Container));
            serializer.Settings.RegisterTagMapping("Repeat", typeof(RepeatSpec.Container));

            //Facade types
            serializer.Settings.RegisterTagMapping("Access", typeof(AccessConstraint.Container));

            //Utility types
            serializer.Settings.RegisterTagMapping("NormalValue", typeof(NormallyDistributedValue.Container));
            serializer.Settings.RegisterTagMapping("UniformValue", typeof(UniformlyDistributedValue.Container));

            //Ref types
            serializer.Settings.RegisterTagMapping("Num", typeof(NumRef.Container));
            serializer.Settings.RegisterTagMapping("Tagged", typeof(TaggedRef.Container));
            serializer.Settings.RegisterTagMapping("Id", typeof(IdRef.Container));

            return serializer;
        }

        public static BuildingDesigner Deserialize(TextReader reader)
        {
            var s = CreateSerializer();

            return s.Deserialize<Container>(reader).Unwrap();
        }

        internal class Container
        {
            public List<string> Tags { get; set; }

            public List<object> Aliases { get; set; }

            public VerticalElementSpec.Container[] Verticals { get; [UsedImplicitly]set; }
            public FacadeSpec.Container[] Facades { get; [UsedImplicitly]set; }
            public ISelectorContainer[] Floors { get; [UsedImplicitly]set; }

            public BuildingDesigner Unwrap()
            {
                return new BuildingDesigner(
                    Floors.Select(a => a.Unwrap()).ToArray(),
                    (Verticals ?? new VerticalElementSpec.Container[0]).Select(a => a.Unwrap()).ToArray(),
                    (Facades ?? new FacadeSpec.Container[0]).Select(a => a.Unwrap()).ToArray()
                );
            }
        }
        #endregion
    }
}
