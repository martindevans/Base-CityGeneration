using System.Collections.Generic;
using System.Linq;

namespace Base_CityGeneration.Elements.Building.Design.Spec.Ref
{
    public class NumRef
        : BaseRef
    {
        public int Number { get; private set; }

        public NumRef(int number, RefFilter filter, bool nonOverlapping, bool inclusive)
            : base(filter, nonOverlapping, inclusive)
        {
            Number = number;
        }

        protected override IEnumerable<FloorSelection> MatchImpl(int basements, IList<FloorSelection> floors, int? startIndex)
        {
            //Find floor with this index
            var matched = floors.SingleOrDefault(a => a.Index == Number);

            //If we matched nothing, return nothing
            if (matched == null)
                yield break;

            //Return the match
            yield return matched;
        }

        internal class Container
            : BaseContainer
        {
            public int N { get; set; }

            private BaseRef _cached;

            public override BaseRef Unwrap()
            {
                if (_cached == null)
                    _cached = new NumRef(N, Filter ?? RefFilter.All, NonOverlapping, Inclusive);
                return _cached;
            }
        }
    }
}
