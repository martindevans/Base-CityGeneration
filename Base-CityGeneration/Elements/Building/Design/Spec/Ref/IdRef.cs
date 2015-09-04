using System.Collections.Generic;
using System.Linq;

namespace Base_CityGeneration.Elements.Building.Design.Spec.Ref
{
    public class IdRef
        : BaseRef
    {
        public string ID { get; private set; }

        public SearchDirection Direction { get; private set; }

        public IdRef(string id, SearchDirection direction, RefFilter filter, bool nonOverlapping, bool inclusive)
            : base(filter, nonOverlapping, inclusive)
        {
            ID = id;
            Direction = direction;
        }

        protected override IEnumerable<FloorSelection> MatchImpl(int basements, IList<FloorSelection> floors, int? startIndex)
        {
            IEnumerable<FloorSelection> set = Prefilter(floors, startIndex, Direction);

            var results = set.Where(a => a.Id == ID);

            return results;
        }

        internal class Container
            : BaseContainer
        {
            public string Id { get; set; }

            public SearchDirection Search { get; set; }

            private IdRef _cached;

            public override BaseRef Unwrap()
            {
                if (_cached == null)
                    _cached = new IdRef(Id, Search, Filter ?? RefFilter.All, NonOverlapping, Inclusive);
                return _cached;
            }
        }
    }
}
