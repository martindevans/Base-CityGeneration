
using System;
using System.Collections.Generic;
using System.Linq;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Selection.Spec.Ref
{
    public class IdRef
        : IRef
    {
        public string ID { get; private set; }

        public SearchDirection Direction { get; private set; }

        private IdRef(string id, SearchDirection direction)
        {
            ID = id;
            Direction = direction;
        }

        public IEnumerable<FloorSelection> Match(int basements, FloorSelection[] floors)
        {
            //Search direction, requires a start point.
            //This can just be an index in the floors array to use as a start point
            throw new NotImplementedException("Implement search direction");

            return floors.Where(a => a.Id == ID);
        }

        internal class Container
            : IRefContainer
        {
            public string Id { get; set; }

            public SearchDirection Search { get; set; }

            private IdRef _cached;

            public IRef Unwrap()
            {
                if (_cached == null)
                    _cached = new IdRef(Id, Search);
                return _cached;
            }
        }
    }
}
