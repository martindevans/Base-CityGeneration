using System.Collections.Generic;
using System.Linq;
using Base_CityGeneration.Elements.Building.Design.Spec.FacadeConstraints;
using Base_CityGeneration.Elements.Building.Design.Spec.Ref;
using Base_CityGeneration.Utilities;
using JetBrains.Annotations;

namespace Base_CityGeneration.Elements.Building.Design.Spec
{
    public class FacadeSpec
    {
        public BaseFacadeConstraint[] Constraints { get; private set; }

        private readonly KeyValuePair<float, KeyValuePair<string, string>[]>[] _tags;
        public IEnumerable<KeyValuePair<float, KeyValuePair<string, string>[]>> Tags
        {
            get
            {
                return _tags;
            }
        }

        public BaseRef Bottom { get; private set; }
        public BaseRef Top { get; private set; }

        private FacadeSpec(BaseFacadeConstraint[] constraints, KeyValuePair<float, KeyValuePair<string, string>[]>[] tags, BaseRef bottom, BaseRef top)
        {
            Constraints = constraints;
            Bottom = bottom;
            Top = top;

            _tags = tags;
        }

        internal class Container
        {
            public TagContainerContainer Tags { get; [UsedImplicitly]set; }

            public BaseFacadeConstraint.BaseContainer[] Constraints { get; [UsedImplicitly]set; }

            public BaseRef.BaseContainer Bottom { get; [UsedImplicitly]set; }
            public BaseRef.BaseContainer Top { get; [UsedImplicitly]set; }

            public FacadeSpec Unwrap()
            {
                return new FacadeSpec(
                    (Constraints ?? new BaseFacadeConstraint.BaseContainer[0]).Select(a => a.Unwrap()).ToArray(),
                    Tags.Unwrap().ToArray(),
                    Bottom.Unwrap(),
                    Top.Unwrap()
                );
            }
        }
    }
}
