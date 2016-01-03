using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Base_CityGeneration.Elements.Building.Design.Spec.Ref
{
    public class RegexIdRef
        : BaseRef
    {
        public string Pattern { get; private set; }

        public SearchDirection Direction { get; private set; }

        public RegexIdRef(string pattern, SearchDirection direction, RefFilter filter, bool nonOverlapping, bool inclusive)
            : base(filter, nonOverlapping, inclusive)
        {
            Pattern = pattern;
            Direction = direction;
        }

        protected override IEnumerable<FloorSelection> MatchImpl(IReadOnlyList<FloorSelection> floors, int? startIndex)
        {
            IEnumerable<FloorSelection> set = Prefilter(floors, startIndex, Direction);

            var results = set.Where(a => Regex.IsMatch(a.Id, Pattern));

            return results;
        }

        internal class Container
            : BaseContainer
        {
            public string Pattern { get; set; }

            public SearchDirection Search { get; set; }

            private RegexIdRef _cached;

            public override BaseRef Unwrap()
            {
                if (_cached == null)
                    _cached = new RegexIdRef(Pattern, Search, Filter ?? RefFilter.All, NonOverlapping, Inclusive);
                return _cached;
            }
        }
    }
}
