
using System.Collections.Generic;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Selection.Spec.Ref
{
    public class NumRef
        : BaseRef
    {
        public int Number { get; private set; }

        public NumRef(int number, RefFilter filter, bool nonOverlapping)
            : base(filter, nonOverlapping)
        {
            Number = number;
        }

        protected override IEnumerable<FloorSelection> MatchImpl(int basements, FloorSelection[] floors, int? startIndex)
        {
            //floors are top to bottom, so we need to convert the floor index
            int index = floors.Length - 1 - basements - Number;

            //If the index is out of range, match nothing
            if (index < 0 || index >= floors.Length)
                yield break;

            //Found a match!
            yield return floors[index];
        }

        internal class Container
            : BaseContainer
        {
            public int N { get; set; }

            private BaseRef _cached;

            public override BaseRef Unwrap()
            {
                if (_cached == null)
                    _cached = new NumRef(N, Filter ?? RefFilter.All, NonOverlapping);
                return _cached;
            }
        }
    }
}
