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
        public int OptionalCount { get; private set; }
        public int RequiredCount { get; private set; }
        public ISpaceSpecProducer Spec { get; set; }

        private RepeatSpec(int optionalCount, int requiredCount, ISpaceSpecProducer spec)
        {
            OptionalCount = optionalCount;
            RequiredCount = requiredCount;
            Spec = spec;
        }

        public IEnumerable<BaseSpaceSpec> Produce(bool required, Func<double> random, INamedDataCollection metadata)
        {
            var counter = required ? RequiredCount : OptionalCount;

            for (var i = 0; i < counter; i++)
                foreach (var spec in Spec.Produce(required, random, metadata))
                    yield return spec;
        }

        internal class Container
            : ISpaceSpecProducerContainer
        {
            public object Optional { get; set; }
            public object Required { get; set; }

            public ISpaceSpecProducerContainer Room { get; set; }

            public ISpaceSpecProducer Unwrap(Func<double> random, INamedDataCollection metadata)
            {
                return new RepeatSpec(
                    IValueGeneratorContainer.FromObject(Optional ?? 0).SelectIntValue(random, metadata),
                    IValueGeneratorContainer.FromObject(Required ?? 1).SelectIntValue(random, metadata),
                    Room.Unwrap(random, metadata)
                );
            }
        }
    }
}
