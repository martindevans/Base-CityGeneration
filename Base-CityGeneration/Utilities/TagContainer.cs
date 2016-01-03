using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Base_CityGeneration.Elements.Building.Design;
using EpimetheusPlugins.Procedural;
using EpimetheusPlugins.Scripts;

namespace Base_CityGeneration.Utilities
{
    internal class TagContainerContainer
        : IDictionary<float, Dictionary<string, string>>
    {
        private readonly List<KeyValuePair<float, Dictionary<string, string>>> _data = new List<KeyValuePair<float, Dictionary<string, string>>>();

        #region implementation
        public IEnumerator<KeyValuePair<float, Dictionary<string, string>>> GetEnumerator()
        {
            return _data.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_data).GetEnumerator();
        }

        public void Add(KeyValuePair<float, Dictionary<string, string>> item)
        {
            _data.Add(item);
        }

        public void Clear()
        {
            _data.Clear();
        }

        public bool Contains(KeyValuePair<float, Dictionary<string, string>> item)
        {
            return _data.Contains(item);
        }

        public void CopyTo(KeyValuePair<float, Dictionary<string, string>>[] array, int arrayIndex)
        {
            _data.CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<float, Dictionary<string, string>> item)
        {
            return _data.Remove(item);
        }

        public int Count
        {
            get { return _data.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool ContainsKey(float key)
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            return _data.Any(a => a.Key == key);
        }

        public void Add(float key, Dictionary<string, string> value)
        {
            _data.Add(new KeyValuePair<float, Dictionary<string, string>>(key, value));
        }

        public bool Remove(float key)
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            return _data.RemoveAll(a => a.Key == key) > 0;
        }

        public bool TryGetValue(float key, out Dictionary<string, string> value)
        {
            foreach (var item in _data)
            {
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (item.Key == key)
                {
                    value = item.Value;
                    return true;
                }
            }

            value = default(Dictionary<string, string>);
            Contract.Assume(!ContainsKey(key));
            return false;
        }

        public Dictionary<string, string> this[float key]
        {
            get
            {
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                return _data.First(a => a.Key == key).Value;
            }
            set { throw new NotSupportedException(); }
        }

        public ICollection<float> Keys
        {
            get { return _data.Select(a => a.Key).ToArray(); }
        }

        public ICollection<Dictionary<string, string>> Values
        {
            get { return _data.Select(a => a.Value).ToArray(); }
        }
        #endregion

        public IEnumerable<KeyValuePair<float, KeyValuePair<string, string>[]>> Unwrap()
        {
            foreach (var item in _data)
                yield return new KeyValuePair<float, KeyValuePair<string, string>[]>(item.Key, item.Value == null ? null : item.Value.ToArray());
        }
    }

    internal static class TagsContainerExtensions
    {
        public static ScriptReference SelectScript(
            this IEnumerable<KeyValuePair<float, KeyValuePair<string, string>[]>> tagsSets,
            Func<double> random,
            Func<KeyValuePair<string, string>[], Type[], ScriptReference> finder,
            out KeyValuePair<string, string>[] tags,
            params Type[] extraConstraints
        )
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
                var script = finder(tags, extraConstraints);

                //If we found something we're good to go
                if (script != null)
                    return script;

                //Failed to find anything, remove this set and try again
                var t = tags;
                options.RemoveAll(a => a.Value == t);
            }

            tags = null;
            throw new DesignFailedException("No suitable script found for any tag set");
        }
    }
}
