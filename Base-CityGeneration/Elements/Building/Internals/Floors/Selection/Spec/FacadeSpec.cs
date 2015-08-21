using Base_CityGeneration.Elements.Building.Internals.Floors.Selection.Spec.FacadeConstraints;
using Base_CityGeneration.Elements.Building.Internals.Floors.Selection.Spec.Ref;
using Base_CityGeneration.Utilities;
using System.Collections.Generic;
using System.Linq;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Selection.Spec
{
    public class FacadeSpec
    {
        public BaseFacadeConstraint[] Constraints { get; private set; }

        private readonly KeyValuePair<float, string[]>[] _tags;
        public IEnumerable<KeyValuePair<float, string[]>> Tags
        {
            get
            {
                return _tags;
            }
        }

        public BaseRef BottomFloor { get; private set; }
        public BaseRef TopFloor { get; private set; }

        private FacadeSpec(BaseFacadeConstraint[] constraints, KeyValuePair<float, string[]>[] tags, BaseRef bottom, BaseRef top)
        {
            Constraints = constraints;
            BottomFloor = bottom;
            TopFloor = top;

            _tags = tags;
        }

        internal class Container
        {
            public TagContainer Tags { get; set; }

            public BaseFacadeConstraint.BaseContainer[] Constraints { get; set; }

            public BaseRef.BaseContainer Bottom { get; set; }
            public BaseRef.BaseContainer Top { get; set; }

            public FacadeSpec Unwrap()
            {
                return new FacadeSpec(
                    (Constraints ?? new BaseFacadeConstraint.BaseContainer[0]).Select(a => a.Unwrap()).ToArray(),
                    Tags.ToArray(),
                    Bottom.Unwrap(),
                    Top.Unwrap()
                );
            }
        }
    }
}
