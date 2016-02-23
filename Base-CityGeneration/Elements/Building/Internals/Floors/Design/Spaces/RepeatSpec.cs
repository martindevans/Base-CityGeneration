using System;
using System.Collections.Generic;
using Base_CityGeneration.Utilities.Extensions;
using Base_CityGeneration.Utilities.Numbers;
using JetBrains.Annotations;
using Myre.Collections;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Design.Spaces
{
    public class RepeatSpec
        : BaseSpaceSpec, IProviderSpec
    {
        public IValueGenerator Count { get; private set; }
        public ISpec Space { get; private set; }

        public RepeatSpec(string id, IValueGenerator count, ISpec space)
            : base(id)
        {
            Count = count;
            Space = space;
        }

        public IEnumerable<ISpec> Expand(Func<double> random, INamedDataCollection metadata)
        {
            var count = Count.SelectIntValue(random, metadata);
            for (var i = 0; i < count; i++)
                yield return Space;
        }

        internal class Container
            : BaseContainer
        {
            public object Count { get; [UsedImplicitly] set; }
            public BaseContainer Space { get; [UsedImplicitly]set; }

            public override BaseSpaceSpec Unwrap()
            {
                return new RepeatSpec(
                    Id,
                    IValueGeneratorContainer.FromObject(Count),
                    Space.Unwrap()
                );
            }
        }
    }
}
