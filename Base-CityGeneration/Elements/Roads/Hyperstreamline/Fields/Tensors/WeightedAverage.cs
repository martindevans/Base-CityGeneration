using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Xna.Framework;

namespace Base_CityGeneration.Elements.Roads.Hyperstreamline.Fields.Tensors
{
    public class WeightedAverage
        : ITensorField
    {
        private float _totalWeight;
        private readonly List<KeyValuePair<ITensorField, float>> _blends = new List<KeyValuePair<ITensorField, float>>();  

        public void Blend(ITensorField field, float weight = 1)
        {
            _blends.Add(new KeyValuePair<ITensorField, float>(field, weight));
            _totalWeight += weight;
        }

        public void Sample(ref Vector2 position, out Tensor result)
        {
            result = new Tensor(0, 0);

            foreach (var b in _blends)
                result += (b.Value / _totalWeight) * b.Key.Sample(position);
        }

        internal class Container
            : ITensorFieldContainer
        {
            public FieldContainer Tensors { get; set; }

            public ITensorField Unwrap(Func<double> random)
            {
                var wa = new WeightedAverage();
                foreach (var tensorFieldContainer in Tensors)
                    wa.Blend(tensorFieldContainer.Value.Unwrap(random), tensorFieldContainer.Key);
                return wa;
            }

            internal class FieldContainer
                : IDictionary<float, ITensorFieldContainer>
            {
                private readonly List<KeyValuePair<float, ITensorFieldContainer>> _data = new List<KeyValuePair<float, ITensorFieldContainer>>();

                public IEnumerator<KeyValuePair<float, ITensorFieldContainer>> GetEnumerator()
                {
                    return _data.GetEnumerator();
                }

                IEnumerator IEnumerable.GetEnumerator()
                {
                    return GetEnumerator();
                }

                public void Add(KeyValuePair<float, ITensorFieldContainer> item)
                {
                    _data.Add(item);
                }

                public void Clear()
                {
                    _data.Clear();
                }

                public bool Contains(KeyValuePair<float, ITensorFieldContainer> item)
                {
                    return _data.Contains(item);
                }

                public void CopyTo(KeyValuePair<float, ITensorFieldContainer>[] array, int arrayIndex)
                {
                    _data.CopyTo(array, arrayIndex);
                }

                public bool Remove(KeyValuePair<float, ITensorFieldContainer> item)
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

                public void Add(float key, ITensorFieldContainer value)
                {
                    Add(new KeyValuePair<float, ITensorFieldContainer>(key, value));
                }

                public bool Remove(float key)
                {
                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    return _data.RemoveAll(a => a.Key == key) > 0;
                }

                public bool TryGetValue(float key, out ITensorFieldContainer value)
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

                public ITensorFieldContainer this[float key]
                {
                    get
                    {
                        ITensorFieldContainer v;
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

                public ICollection<ITensorFieldContainer> Values
                {
                    get
                    {
                        return _data.Select(a => a.Value).ToArray();
                    }
                }
            }
        }
    }
}
