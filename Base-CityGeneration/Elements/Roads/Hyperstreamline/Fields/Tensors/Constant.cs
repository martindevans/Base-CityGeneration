﻿using System;
using System.Numerics;
using Myre.Collections;

namespace Base_CityGeneration.Elements.Roads.Hyperstreamline.Fields.Tensors
{
    public class Constant
        : ITensorField
    {
        private readonly Tensor _value;

        public Constant(Tensor value)
        {
            _value = value;
        }

        public void Sample(ref Vector2 position, out Tensor result)
        {
            result = _value;
        }

        internal class Container
            : ITensorFieldContainer
        {
            public Tensor Value { get; set; }

            public ITensorField Unwrap(Func<double> random, INamedDataCollection metadata)
            {
                return new Constant(Value);
            }
        }
    }
}
