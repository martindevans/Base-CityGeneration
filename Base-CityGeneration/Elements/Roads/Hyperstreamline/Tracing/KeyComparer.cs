using System;
using System.Collections.Generic;

namespace Base_CityGeneration.Elements.Roads.Hyperstreamline.Tracing
{
    class KeyComparer<TKey, TValue>
        : IComparer<KeyValuePair<TKey, TValue>>
        where TKey : IComparable<TKey>
    {
        public int Compare(KeyValuePair<TKey, TValue> x, KeyValuePair<TKey, TValue> y)
        {
            return x.Key.CompareTo(y.Key);
        }
    }
}
