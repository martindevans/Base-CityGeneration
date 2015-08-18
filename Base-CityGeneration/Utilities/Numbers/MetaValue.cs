using Base_CityGeneration.Styles;
using Microsoft.Xna.Framework;
using Myre;
using Myre.Collections;
using System;

namespace Base_CityGeneration.Utilities.Numbers
{
    public class MetaValue
        : IValueGenerator
    {
        private readonly string _name;
        private readonly IValueGenerator _defaultValue;

        public MetaValue(string name, IValueGenerator defaultValue)
        {
            _name = name;
            _defaultValue = defaultValue;
        }

        public float SelectFloatValue(Func<double> random, INamedDataCollection data)
        {
            return data.DetermineHierarchicalValue(
                new TypedName<float>(_name),
                old => old,
                () => _defaultValue.SelectFloatValue(random, data)
            );
        }

        public int SelectIntValue(Func<double> random, INamedDataCollection data)
        {
            return data.DetermineHierarchicalValue(
                new TypedName<int>(_name),
                old => old,
                () => _defaultValue.SelectIntValue(random, data)
            );
        }

        internal class Container
            : BaseValueGeneratorContainer
        {
            public string Name { get; set; }

            public object Default { get; set; }

            protected override IValueGenerator UnwrapImpl()
            {
                return new MetaValue(Name, FromObject(Default ?? 0));
            }
        }
    }
}
