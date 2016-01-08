using System;
using JetBrains.Annotations;
using Myre.Collections;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Design.Connections
{
    public class RegexIdRef
        : BaseSpaceConnectionSpec
    {
        private readonly string _pattern;
        public string Pattern { get { return _pattern; } }

        public RegexIdRef(string pattern)
        {
            _pattern = pattern;
        }

        internal class Container
            : BaseContainer
        {
            public string Pattern { get; [UsedImplicitly]set; }

            public override BaseSpaceConnectionSpec Unwrap(Func<double> random, INamedDataCollection metadata)
            {
                return new RegexIdRef(Pattern);
            }
        }
    }
}
