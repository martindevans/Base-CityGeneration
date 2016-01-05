using System;
using System.Collections.Generic;
using Base_CityGeneration.Utilities.Extensions;
using Base_CityGeneration.Utilities.Numbers;
using Myre.Collections;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Design.Spaces
{
    public class RepeatSpec
        : ISpaceSpecProducer
    {
        public IValueGenerator OptionalCount { get; set; }
        public IValueGenerator RequiredCount { get; set; }
        public ISpaceSpecProducer Spec { get; set; }

        private RepeatSpec(IValueGenerator optionalCount, IValueGenerator requiredCount, ISpaceSpecProducer spec)
        {
            OptionalCount = optionalCount;
            RequiredCount = requiredCount;
            Spec = spec;
        }

        public IEnumerable<BaseSpaceSpec> Produce(bool required, Func<double> random, INamedDataCollection metadata)
        {
            var counter = required ? RequiredCount : OptionalCount;
            var count = counter.SelectIntValue(random, metadata);

            for (var i = 0; i < count; i++)
                foreach (var spec in Spec.Produce(required, random, metadata))
                    yield return spec;
        }

        internal class Container
            : ISpaceSpecProducerContainer
        {
            public object Optional { get; set; }
            public object Required { get; set; }

            public ISpaceSpecProducerContainer Room { get; set; }

            public ISpaceSpecProducer Unwrap()
            {
                return new RepeatSpec(
                    IValueGeneratorContainer.FromObject(Optional ?? 0),
                    IValueGeneratorContainer.FromObject(Required ?? 1),
                    Room.Unwrap()
                );
            }
        }
    }
}
