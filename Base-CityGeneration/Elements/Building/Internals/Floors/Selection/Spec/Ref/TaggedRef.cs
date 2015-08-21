using System.Collections.Generic;
using System.Linq;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Selection.Spec.Ref
{
    public class TaggedRef
        : BaseRef
    {
        private readonly string[] _tags;
        public IEnumerable<string> Tags { get { return _tags; } }

        private readonly RefFilter _filter;

        public TaggedRef(string[] tags, RefFilter filter, bool nonOverlapping)
            : base(filter, nonOverlapping)
        {
            _tags = tags;
            _filter = filter;
        }

        protected override IEnumerable<FloorSelection> MatchImpl(int basements, FloorSelection[] floors, int? startIndex)
        {
            //If (TagsWeWant - TagsFloorHas) is empty then obviously the floor has all the tags we want
            return floors.Where(a => !_tags.Except(a.Tags).Any());
        }

        internal class Container
            : BaseContainer
        {
            public string[] Tags { get; set; }

            private BaseRef _cached;

            public override BaseRef Unwrap()
            {
                if (_cached == null)
                    _cached = new TaggedRef(Tags, Filter ?? RefFilter.All, NonOverlapping);
                return _cached;
            }
        }
    }
}
