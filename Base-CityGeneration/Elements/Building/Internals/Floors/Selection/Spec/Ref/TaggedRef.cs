using System.Collections.Generic;
using System.Linq;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Selection.Spec.Ref
{
    public class TaggedRef
        : IRef
    {
        private readonly string[] _tags;
        public IEnumerable<string> Tags { get { return _tags; } }

        public VerticalElementCreationOptions Filter { get; private set; }

        private TaggedRef(string[] tags, VerticalElementCreationOptions filter)
        {
            Filter = filter;
            _tags = tags;
        }

        public IEnumerable<FloorSelection> Match(int basements, FloorSelection[] floors, int? startIndex)
        {
            //If (TagsWeWant - TagsFloorHas) is empty then obviously the floor has all the tags we want
            return floors.Where(a => !_tags.Except(a.Tags).Any());
        }

        internal class Container
            : IRefContainer
        {
            public string[] Tags { get; set; }
            public VerticalElementCreationOptions? Filter { get; set; }

            private IRef _cached;

            public IRef Unwrap()
            {
                if (_cached == null)
                    _cached = new TaggedRef(Tags, Filter ?? VerticalElementCreationOptions.All);
                return _cached;
            }
        }
    }
}
