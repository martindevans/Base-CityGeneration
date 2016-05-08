using System;
using System.Collections.Generic;
using System.Numerics;
using JetBrains.Annotations;
using Myre;
using Myre.Collections;

namespace Base_CityGeneration.Elements.Building.Design.Spec.Markers.Algorithms.Conditionals
{
    public class MetaPredicate
        : BaseFootprintAlgorithm
    {
        private readonly string _key;
        private readonly BaseFootprintAlgorithm _pass;
        private readonly BaseFootprintAlgorithm _fail;

        public MetaPredicate(string key, BaseFootprintAlgorithm pass, BaseFootprintAlgorithm fail)
        {
            _key = key;
            _pass = pass;
            _fail = fail;
        }

        public override IReadOnlyList<Vector2> Apply(Func<double> random, INamedDataCollection metadata, IReadOnlyList<Vector2> footprint, IReadOnlyList<Vector2> basis, IReadOnlyList<Vector2> lot)
        {
            return
                (metadata.GetValue(new TypedName<bool>(_key)) ? _pass : _fail)
                .Apply(random, metadata, footprint, basis, lot);
        }

        internal class Container
            : BaseContainer
        {
            public string Key { get; [UsedImplicitly]set; }
            public BaseContainer Pass { get; [UsedImplicitly]set; }
            public BaseContainer Fail { get; [UsedImplicitly]set; }

            public override BaseFootprintAlgorithm Unwrap()
            {
                return new MetaPredicate(
                    Key,
                    Pass.UnwrapNullable() ?? new Identity(),
                    Fail.UnwrapNullable() ?? new Identity()
                );
            }
        }
    }
}
