using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Numerics;
using Myre.Collections;

namespace Base_CityGeneration.Elements.Roads.Hyperstreamline.Fields.Tensors
{
    public class Addition
        : ITensorField
    {
        private readonly List<ITensorField> _fields = new List<ITensorField>();

        public Addition(params ITensorField[] fields)
        {
            Contract.Requires(fields != null);

            foreach (var tensorField in fields)
                Add(tensorField);
        }

        [ContractInvariantMethod]
        private void ObjectInvariant()
        {
            Contract.Invariant(_fields != null);
        }

        public void Add(ITensorField field)
        {
            Contract.Requires(field != null);

            _fields.Add(field);
        }

        public void Sample(ref Vector2 position, out Tensor result)
        {
            result = new Tensor(0, 0);

            foreach (var b in _fields)
                result += b.Sample(position);
        }

        internal class Container
            : ITensorFieldContainer
        {
            public ITensorFieldContainer[] Tensors { get; set; }

            public ITensorField Unwrap(Func<double> random, INamedDataCollection metadata)
            {
                return new Addition(Tensors.Select(a => a.Unwrap(random, metadata)).ToArray());
            }
        }
    }
}
