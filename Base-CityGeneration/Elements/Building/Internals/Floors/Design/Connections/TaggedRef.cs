using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Myre.Collections;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Design.Connections
{
    public class TaggedRef
        : BaseSpaceConnectionSpec
    {
        private readonly KeyValuePair<string, string>[] _tags;
        public IEnumerable<KeyValuePair<string, string>> Tags { get { return _tags; } }

        public TaggedRef(KeyValuePair<string, string>[] tags)
        {
            _tags = tags;
        }

        internal class Container
            : BaseContainer
        {
            public KeyValuePair<string, string>[] Tags { get; [UsedImplicitly]set; }

            public override BaseSpaceConnectionSpec Unwrap(Func<double> random, INamedDataCollection metadata)
            {
                return new TaggedRef(Tags);
            }
        }
    }
}
