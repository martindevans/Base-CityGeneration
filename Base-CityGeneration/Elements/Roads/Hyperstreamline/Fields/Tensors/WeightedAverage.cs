using System;
using System.Collections.Generic;
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
            public Dictionary<float, ITensorFieldContainer> Tensors { get; set; }

            public ITensorField Unwrap(Func<double> random)
            {
                var wa = new WeightedAverage();
                foreach (var tensorFieldContainer in Tensors)
                    wa.Blend(tensorFieldContainer.Value.Unwrap(random), tensorFieldContainer.Key);
                return wa;
            }
        }
    }
}
