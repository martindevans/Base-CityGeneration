﻿using System.Linq;
using Base_CityGeneration.Elements.Building.Design.Spec.Markers;
using Base_CityGeneration.Utilities;
using EpimetheusPlugins.Scripts;
using Myre.Collections;
using System;
using System.Collections.Generic;
using Base_CityGeneration.Elements.Building.Internals.Floors;

namespace Base_CityGeneration.Elements.Building.Design.Spec
{
    public abstract class BaseFloorSelector
    {
        public abstract float MinHeight { get; }

        public abstract float MaxHeight { get; }

        public abstract IEnumerable<FloorRun> Select(Func<double> random, INamedDataCollection metadata, Func<KeyValuePair<string, string>[], Type[], ScriptReference> finder);

        protected FloorSelection SelectSingle(Func<double> random, IEnumerable<KeyValuePair<float, KeyValuePair<string, string>[]>> tags, Func<KeyValuePair<string, string>[], Type[], ScriptReference> finder, float height, string id)
        {
            KeyValuePair<string, string>[] selectedTags;
            ScriptReference script = tags.SelectScript(random, finder, out selectedTags, typeof(IFloor));
            if (script == null)
                return null;

            return new FloorSelection(id, selectedTags, this, script, height);
        }
    }

    /// <summary>
    /// A set of floors, with sequential indices, all sharing a single footprint
    /// </summary>
    public class FloorRun
    {
        /// <summary>
        /// The floors in this run
        /// </summary>
        public readonly IReadOnlyList<FloorSelection> Selection;

        /// <summary>
        /// The footprint of this run (nearest the ground end of the run)
        /// </summary>
        public readonly BaseMarker Marker;

        public FloorRun(FloorSelection[] floors, BaseMarker marker)
        {
            Selection = floors;
            Marker = marker;
        }

        public FloorRun Clone()
        {
            return new FloorRun(
                Selection.Select(a => a.Clone()).ToArray(),
                Marker
            );
        }
    }

    internal interface ISelectorContainer
    {
        BaseFloorSelector Unwrap();
    }
}
