using System;
using System.Collections.Generic;
using System.Numerics;
using JetBrains.Annotations;
using Myre;
using Myre.Collections;
using Myre.Extensions;

namespace Base_CityGeneration.Elements.Building.Design.Spec.Markers.Algorithms
{
    public class MetaSet<T>
        : BaseFootprintAlgorithm
    {
        private readonly string _key;
        private readonly T _value;

        public MetaSet(string key, T value)
        {
            _key = key;
            _value = value;
        }

        public override IReadOnlyList<Vector2> Apply(Func<double> random, INamedDataCollection metadata, IReadOnlyList<Vector2> footprint, IReadOnlyList<Vector2> basis, IReadOnlyList<Vector2> lot)
        {
            metadata.Set(new TypedName<T>(_key), _value);

            return footprint;
        }
    }

    internal abstract class MetaSet
    {
        internal class Container
            : BaseFootprintAlgorithm.BaseContainer
        {
            public string Type { get; [UsedImplicitly]set; }
            public string Key { get; [UsedImplicitly]set; }
            public string Value { get; [UsedImplicitly]set; }

            public override BaseFootprintAlgorithm Unwrap()
            {
                var type = System.Type.GetType(Type, true, true);
                var value = type.Parse(Value);

                // ReSharper disable PossibleNullReferenceException
                //Justification: Any NRE here is a programmer error!
                return (BaseFootprintAlgorithm)typeof(MetaSet<>).MakeGenericType(type).GetConstructor(new[] {
                    typeof(string), type
                }).Invoke(new object[] { Key, value });
                // ReSharper restore PossibleNullReferenceException
            }
        }
    }
}
