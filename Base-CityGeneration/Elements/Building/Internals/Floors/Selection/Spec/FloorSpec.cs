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

        private readonly HeightSpec _height;
        public HeightSpec Height
        {
            get
            {
                return _height;
            }
        }

        public FloorSpec(KeyValuePair<float, string[]>[] tags, HeightSpec height)
        {
            _tags = tags;

            _height = height;
        }

        public IEnumerable<FloorSelection> Select(Func<double> random, ScriptReference[] verticals, Func<string[], ScriptReference> finder, IGroupFinder groupFinder)
        {
            var selected = SelectSingle(random, _tags, verticals, finder);
            if (selected == null)
                return new FloorSelection[0];

            var height = _height.SelectHeight(random, groupFinder);
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
            public TagContainer Tags { get; set; }

            public HeightSpec.Container Height { get; set; }

            public ISelector Unwrap()
            {
                return new FloorSpec(Tags.ToArray(), Height.Unwrap());
            }
        }
    }
}
