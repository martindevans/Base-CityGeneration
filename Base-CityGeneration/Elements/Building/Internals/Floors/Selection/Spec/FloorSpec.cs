using System;
using System.Collections.Generic;
using System.Linq;
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

        private readonly NormalValueSpec _height;
        public NormalValueSpec Height
        {
            get
            {
                return _height;
            }
        }

        private readonly string _id;
        public string Id { get { return _id; } }

        public FloorSpec(KeyValuePair<float, string[]>[] tags, NormalValueSpec height)
            : this(Guid.NewGuid().ToString(), tags, height)
        {
        }

        public FloorSpec(string id, KeyValuePair<float, string[]>[] tags, NormalValueSpec height)
        {
            _id = id;
            _tags = tags;
            _height = height;
        }

        public IEnumerable<FloorSelection> Select(Func<double> random, ScriptReference[] verticals, Func<string[], ScriptReference> finder, IGroupFinder groupFinder)
        {
            var selected = SelectSingle(random, _tags, verticals, finder);
            if (selected == null)
                return new FloorSelection[0];

            var height = _height.SelectFloatValue(random, groupFinder);
            return new FloorSelection[] { new FloorSelection(selected, height) };
        }

        public static ScriptReference SelectSingle(Func<double> random, IEnumerable<KeyValuePair<float, string[]>> tags, ScriptReference[] verticals, Func<string[], ScriptReference> finder)
        {
            List<KeyValuePair<float, string[]>> tagSets = new List<KeyValuePair<float, string[]>>(tags);

            while (tagSets.Count > 0)
            {
                //Select a set
                var set = tagSets.WeightedRandom(random);

                // Find a script (null tags set means explicitly select no script)
                if (set == null)
                    return null;
                var selected = finder(set);

                //If we found something we're good to go
                if (selected != null)
                    return selected;

                //Failed to find anything, remove this set and try again
                tagSets.RemoveAt(tagSets.FindIndex(a => a.Value == set));
            }

            throw new SelectionFailedException("No suitable floors found for any tag set");
        }

        internal class Container
            : ISelectorContainer
        {
            public string Id { get; set; }

            public TagContainer Tags { get; set; }

            public NormalValueSpec.Container Height { get; set; }

            public ISelector Unwrap()
            {
                NormalValueSpec height;
                if (Height == null)
                    height = new NormalValueSpec(2.5f, 3f, 3.5f, 0.2f);
                else
                    height = Height.Unwrap();

                return new FloorSpec(Id ?? Guid.NewGuid().ToString(), Tags.ToArray(), height);
            }
        }
    }
}
