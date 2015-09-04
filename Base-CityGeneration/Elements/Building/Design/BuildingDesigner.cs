using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Base_CityGeneration.Elements.Building.Design.Spec;
using Base_CityGeneration.Elements.Building.Design.Spec.FacadeConstraints;
using Base_CityGeneration.Elements.Building.Design.Spec.Markers;
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
        private readonly VerticalElementSpec[] _verticalSelectors;
        public IEnumerable<VerticalElementSpec> VerticalSelectors
        {
            get
            {
                return _verticalSelectors;
            }
        }

        private readonly IFloorSelector[] _floorSelectors;
        public IEnumerable<IFloorSelector> FloorSelectors
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

        public BuildingDesigner(IFloorSelector[] floorSelectors, VerticalElementSpec[] verticalSelectors, FacadeSpec[] facadeSelectors)
        {
            _floorSelectors = floorSelectors;
            _verticalSelectors = verticalSelectors;
            _facadeSelectors = facadeSelectors;
        }

        /// <summary>
        /// Select a design for this building based on the spec
        /// </summary>
        /// <param name="random">random number generator</param>
        /// <param name="metadata">building metadata</param>
        /// <param name="finder">a function which finds scripts from tags</param>
        /// <param name="neighbourHeights">The heights of neighbouring buildings, for each edge of this building</param>
        /// <returns></returns>
        public Selection Select(Func<double> random, INamedDataCollection metadata, Func<string[], ScriptReference> finder, ReadOnlyCollection<float> neighbourHeights)
        {
            var aboveGround = _floorSelectors.TakeWhile(a => !(a is GroundMarker)).ToArray();
            var belowGround = _floorSelectors.SkipWhile(a => !(a is GroundMarker)).ToArray();

            //Select above ground floors, then assign indices
            var above = SelectFloors(random, metadata, finder, aboveGround).ToArray();
            for (int i = 0; i < above.Length; i++)
                above[i] = new FloorSelection(above[i], above.Length - i - 1);

            //Select below ground floors, then assign indices
            var below = SelectFloors(random, metadata, finder, belowGround).ToArray();
            for (int i = 0; i < below.Length; i++)
                below[i] = new FloorSelection(below[i], -(i + 1));

            //Select vertical elements for floors
            var verticals = SelectVerticals(random, finder, _verticalSelectors, above, below).ToArray();

            //Select facades for floors
            var facades = SelectFacades(random, finder, _facadeSelectors, above, neighbourHeights).ToArray();

            //Return the selection, ready to be turned into a building
            return new Selection(
                above,
                below,
                verticals,
                facades
            );
        }

        private static IEnumerable<FloorSelection> SelectFloors(Func<double> random, INamedDataCollection metadata, Func<string[], ScriptReference> finder, IEnumerable<IFloorSelector> selectors)
        {
            List<FloorSelection> floors = new List<FloorSelection>();

            foreach (var selector in selectors)
                floors.AddRange(selector.Select(random, metadata, finder));

            return floors;
        }

        private static IEnumerable<VerticalSelection> SelectVerticals(Func<double> random, Func<string[], ScriptReference> finder, IEnumerable<VerticalElementSpec> verticalSelectors, IEnumerable<FloorSelection> above, IEnumerable<FloorSelection> below)
        {
            var floors = above.Append(below).ToArray();

            List<VerticalSelection> verticals = new List<VerticalSelection>();

            foreach (var selector in verticalSelectors)
                verticals.AddRange(selector.Select(random, finder, below.Count(), floors));

            return verticals;
        }

        private static IEnumerable<IEnumerable<FacadeSelection>> SelectFacades(Func<double> random, Func<string[], ScriptReference> finder, FacadeSpec[] selectors, FloorSelection[] aboveGroundFloors, IEnumerable<float> neighbourHeights)
        {
            foreach (var height in neighbourHeights)
            {
                yield return SelectFacadesForWall(random, finder, selectors, aboveGroundFloors, height);
            }
        }

        private static IEnumerable<FacadeSelection> SelectFacadesForWall(Func<double> random, Func<string[], ScriptReference> finder, IEnumerable<FacadeSpec> specs, FloorSelection[] allFloors, float startHeight)
        {
            //Select floors which are above the required height
            //We keep a list of uninterrupted runs of floors, and then match facades on each run
            //to start with there is just one run
            Stack<List<FloorSelection>> runs = new Stack<List<FloorSelection>>(new[] {
                (from floor in allFloors
                 let below = allFloors.Where(f => f.Index < floor.Index).Select(f => f.Height).Sum()
                 where below >= startHeight
                 select floor).ToList()
            }.Where(r => r.Count > 0));

            //This entire wall is obscured, no facades
            if (runs.Count == 0)
                return new FacadeSelection[0];

            //Set of selected facades
            List<FacadeSelection> facades = new List<FacadeSelection>();

            //Keep applying specs to runs
            while (runs.Count > 0)
            {
                //Choose a run to process
                var run = runs.Pop();

                //Process it, adding a load of facades to the output, as well as producing a new set of runs
                var produced = SelectFacadesForRun(random, finder, specs, run, facades);

                //Add all the new runs to the stack
                foreach (var item in produced)
                    runs.Push(item);
            }

            return facades;
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
                var bot = spec.Bottom.Match(0, run, null);
                var top = spec.Top.MatchFrom(0, run, spec.Bottom, bot);

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

            throw new DesignFailedException("Could not find an applicable facade spec for floor run");
        }

        public class Selection
        {
            /// <summary>
            /// Set of floors to place above ground
            /// </summary>
            public IReadOnlyCollection<FloorSelection> AboveGroundFloors { get; private set; }

            /// <summary>
            /// Set of floors to place below ground
            /// </summary>
            public IReadOnlyCollection<FloorSelection> BelowGroundFloors { get; private set; }

            /// <summary>
            /// Vertical elements to place within this building
            /// </summary>
            public IReadOnlyCollection<VerticalSelection> Verticals { get; private set; }

            /// <summary>
            /// Facades to place around this building, in the same order as the "neighbour heights" supplied to the select method
            /// </summary>
            public IReadOnlyCollection<IReadOnlyCollection<FacadeSelection>> Facades { get; private set; }

            public Selection(FloorSelection[] aboveGroundFloors, FloorSelection[] belowGroundFloors, VerticalSelection[] verticals, IEnumerable<IEnumerable<FacadeSelection>> facades)
            {
                AboveGroundFloors = aboveGroundFloors;
                BelowGroundFloors = belowGroundFloors;
                Verticals = verticals;

                Facades = facades.Select(a => a.ToArray()).ToArray();
            }
        }

        #region serialization
        private static Serializer CreateSerializer()
        {
            var serializer = new Serializer(new SerializerSettings
            {
                EmitTags = true,
            });

            //Floor element types
            serializer.Settings.RegisterTagMapping("Ground", typeof(GroundMarker.Container));
            serializer.Settings.RegisterTagMapping("Building", typeof(Container));
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
