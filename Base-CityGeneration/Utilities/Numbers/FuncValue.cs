using System;
using System.Diagnostics.Contracts;
using Myre.Collections;

namespace Base_CityGeneration.Utilities.Numbers
{
    public class FuncValue
        : IValueGenerator
    {
        private readonly Func<Func<double>, INamedDataCollection, float> _generate;

        public float MaxValue { get; private set; }
        public float MinValue { get; private set; }

        public FuncValue(Func<Func<double>, INamedDataCollection, float> generate, float min, float max)
        {
            Contract.Requires(generate != null);

            _generate = generate;

            MinValue = min;
            MaxValue = max;
        }

        public float SelectFloatValue(Func<double> random, INamedDataCollection data)
        {
            return _generate(random, data);
        }
    }
}
