using System.Collections.Generic;
using System.Linq;

namespace Base_CityGeneration.Elements.Building.Design.Spec.Ref
{
    public class TaggedRef
        : BaseRef
    {
        private readonly KeyValuePair<string, string>[] _tags;
        public IEnumerable<KeyValuePair<string, string>> Tags { get { return _tags; } }

        public TaggedRef(KeyValuePair<string, string>[] tags, RefFilter filter, bool nonOverlapping, bool inclusive)
            : base(filter, nonOverlapping, inclusive)
        {
            _tags = tags;
        }

        protected override IEnumerable<FloorSelection> MatchImpl(IReadOnlyList<FloorSelection> floors, int? startIndex)
        {
            //If (TagsWeWant - TagsFloorHas) is empty then obviously the floor has all the tags we want
            return floors.Where(a => !_tags.Except(a.Tags).Any());
        }

        internal class Container
            : BaseContainer
        {
            public KeyValuePair<string, string>[] Tags { get; set; }

            private BaseRef _cached;

            public override BaseRef Unwrap()
            {
                if (_cached == null)
                    _cached = new TaggedRef(Tags, Filter ?? RefFilter.All, NonOverlapping, Inclusive);
                return _cached;
            }
        }
    }
}
