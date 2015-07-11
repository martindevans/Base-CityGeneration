using System;
using System.Collections.Generic;
using System.Linq;
using Base_CityGeneration.Utilities.Numbers;
using EpimetheusPlugins.Procedural;
using EpimetheusPlugins.Scripts;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Selection.Spec
{
    /// <summary>
    /// Selector for a single floor
    /// </summary>
    public class FloorSpec
        : ISelector
    {
        private readonly KeyValuePair<float, string[]>[] _tags;
        public IEnumerable<KeyValuePair<float, string[]>> Tags
        {
            get
            {
                return _tags;
            }
        }

        private readonly IValueGenerator _height;
        public IValueGenerator Height
        {
            get
            {
                return _height;
            }
        }

        private readonly string _id;
        public string Id { get { return _id; } }

        public FloorSpec(KeyValuePair<float, string[]>[] tags, IValueGenerator height)
            : this(Guid.NewGuid().ToString(), tags, height)
        {
        }

        public FloorSpec(string id, KeyValuePair<float, string[]>[] tags, IValueGenerator height)
        {
            _id = id;
            _tags = tags;
            _height = height;
        }

        public IEnumerable<FloorSelection> Select(Func<double> random, Func<string[], ScriptReference> finder)
        {
            var selected = SelectSingle(random, _tags, finder, _height.SelectFloatValue(random), Id);
            if (selected == null)
                return new FloorSelection[0];

            return new FloorSelection[] { selected.Value };
        }

        public static FloorSelection? SelectSingle(Func<double> random, IEnumerable<KeyValuePair<float, string[]>> tags, Func<string[], ScriptReference> finder, float height, string id)
        {
            string[] selectedTags;
            ScriptReference script = FindScript(random, finder, tags, out selectedTags);
            if (script == null)
                return null;

            return new FloorSelection(id, selectedTags, script, height);
        }

        public static ScriptReference FindScript(Func<double> random, Func<string[], ScriptReference> finder, IEnumerable<KeyValuePair<float, string[]>> tagSets, out string[] tags)
        {
            var options = tagSets.ToList();
            while (options.Count > 0)
            {
                //Select a set
                tags = tagSets.WeightedRandom(random);

                // Find a script (null tags set means explicitly select no script)
                if (tags == null)
                    return null;

                //Find a script for this set
                var script = finder(tags);

                //If we found something we're good to go
                if (script != null)
                    return script;

                //Failed to find anything, remove this set and try again
                var t = tags;
                options.RemoveAll(a => a.Value == t);
            }

            tags = null;
            throw new SelectionFailedException("No suitable script found for any tag set");
        }

        internal class Container
            : ISelectorContainer
        {
            public string Id { get; set; }

            public TagContainer Tags { get; set; }

            public IValueGeneratorContainer Height { get; set; }

            public ISelector Unwrap()
            {
                IValueGenerator height = Height == null ? new NormallyDistributedValue(2.5f, 3f, 3.5f, 0.2f) : Height.Unwrap();

                return new FloorSpec(Id ?? Guid.NewGuid().ToString(), Tags.ToArray(), height);
            }
        }
    }
}
