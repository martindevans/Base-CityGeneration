using System.Collections.Generic;
using System.Linq;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Selection.Spec.Ref
{
    public class IdRef
        : IRef
    {
        public string ID { get; private set; }

        public SearchDirection Direction { get; private set; }

        public VerticalElementCreationOptions Filter { get; private set; }

        public IdRef(string id, SearchDirection direction, VerticalElementCreationOptions filter)
        {
            ID = id;
            Direction = direction;
            Filter = filter;
        }

        public IEnumerable<FloorSelection> Match(int basements, FloorSelection[] floors, int? startIndex)
        {
            var set = Direction == SearchDirection.Down
                ? floors.Skip(startIndex ?? 0)
                : floors.Reverse().Skip(floors.Length - (startIndex ?? floors.Length) - 1);

            var results = set.Where(a => a.Id == ID);

            return results;
        }

        internal class Container
            : IRefContainer
        {
            public string Id { get; set; }
            public SearchDirection Search { get; set; }
            public VerticalElementCreationOptions? Filter { get; set; }

            private IdRef _cached;

            public IRef Unwrap()
            {
                if (_cached == null)
                    _cached = new IdRef(Id, Search, Filter ?? VerticalElementCreationOptions.All);
                return _cached;
            }
        }
    }
}
