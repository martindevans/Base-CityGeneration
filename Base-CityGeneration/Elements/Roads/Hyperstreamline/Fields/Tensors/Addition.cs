using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Base_CityGeneration.Elements.Roads.Hyperstreamline.Fields.Tensors
{
    public class Addition
        : ITensorField
    {
        private readonly List<ITensorField> _fields = new List<ITensorField>();

        public void Add(ITensorField field)
        {
            _fields.Add(field);
        }

        public void Sample(ref Vector2 position, out Tensor result)
        {
            result = new Tensor(0, 0);

            foreach (var b in _fields)
                result += b.Sample(position);
        }
    }
}
