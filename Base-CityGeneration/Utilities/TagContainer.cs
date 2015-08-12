using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Base_CityGeneration.Elements.Building.Internals.Floors.Selection;
using EpimetheusPlugins.Procedural;
using EpimetheusPlugins.Scripts;

namespace Base_CityGeneration.Utilities
{
    class TagContainer
        : IDictionary<float, string[]>
    {
        private readonly List<KeyValuePair<float, string[]>> _data = new List<KeyValuePair<float, string[]>>();  

        public IEnumerator<KeyValuePair<float, string[]>> GetEnumerator()
        {
            return _data.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(KeyValuePair<float, string[]> item)
        {
            _data.Add(item);
        }

        public void Clear()
        {
            _data.Clear();
        }

        public bool Contains(KeyValuePair<float, string[]> item)
        {
            return _data.Contains(item);
        }

        public void CopyTo(KeyValuePair<float, string[]>[] array, int arrayIndex)
        {
            _data.CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<float, string[]> item)
        {
            return _data.Remove(item);
        }

        public int Count
        {
            get
            {
                return _data.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public bool ContainsKey(float key)
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            return _data.FindIndex(a => a.Key == key) != -1;
        }

        public void Add(float key, string[] value)
        {
            Add(new KeyValuePair<float, string[]>(key, value));
        }

        public bool Remove(float key)
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            return _data.RemoveAll(a => a.Key == key) > 0;
        }

        public bool TryGetValue(float key, out string[] value)
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            var i = _data.FindIndex(a => a.Key == key);
            if (i == -1)
            {
                value = null;
                return false;
            }

            value = _data[i].Value;
            return true;
        }

        public string[] this[float key]
        {
            get
            {
                string[] v;
                if (!TryGetValue(key, out v))
                    throw new KeyNotFoundException(key.ToString(CultureInfo.InvariantCulture));
                return v;
            }
            set
            {
                Add(key, value);
            }
        }

        public ICollection<float> Keys
        {
            get
            {
                return _data.Select(a => a.Key).ToArray();
            }
        }

        public ICollection<string[]> Values
        {
            get
            {
                return _data.Select(a => a.Value).ToArray();
            }
        }
    }

    internal static class TagsContainerExtensions
    {
        public static ScriptReference SelectScript(this IEnumerable<KeyValuePair<float, string[]>> tagsSets, Func<double> random, Func<string[], ScriptReference> finder, out string[] tags)
        {
            var options = tagsSets.ToList();
            while (options.Count > 0)
            {
                //Select a set
                tags = options.WeightedRandom(random);

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
    }
}
