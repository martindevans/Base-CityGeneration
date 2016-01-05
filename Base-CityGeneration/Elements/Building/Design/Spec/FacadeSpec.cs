using System.Collections.Generic;
using System.Diagnostics.Contracts;
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
                Contract.Ensures(Contract.Result<IEnumerable<KeyValuePair<float, KeyValuePair<string, string>[]>>>() != null);
                return _tags;
            }
        }

        private readonly BaseRef _bottom;
        public BaseRef Bottom
        {
            get
            {
                Contract.Ensures(Contract.Result<BaseRef>() != null);
                return _bottom;
            }
        }

        private readonly BaseRef _top;
        public BaseRef Top
        {
            get
            {
                Contract.Ensures(Contract.Result<BaseRef>() != null);
                return _top;
            }
        }

        private FacadeSpec(BaseFacadeConstraint[] constraints, KeyValuePair<float, KeyValuePair<string, string>[]>[] tags, BaseRef bottom, BaseRef top)
        {
            Contract.Requires(constraints != null);
            Contract.Requires(Contract.ForAll(constraints, c => c != null));
            Contract.Requires(tags != null);
            Contract.Requires(bottom != null);
            Contract.Requires(top != null);

            Constraints = constraints;
            _bottom = bottom;
            _top = top;

            _tags = tags;
        }

        [ContractInvariantMethod]
        private void ObjectInvariants()
        {
            Contract.Invariant(_tags != null);
            Contract.Invariant(_bottom != null);
            Contract.Invariant(_top != null);
        }

        internal class Container
        {
            public TagContainerContainer Tags { get; [UsedImplicitly]set; }

            public BaseFacadeConstraint.BaseContainer[] Constraints { get; [UsedImplicitly]set; }

            public BaseRef.BaseContainer Bottom { get; [UsedImplicitly]set; }
            public BaseRef.BaseContainer Top { get; [UsedImplicitly]set; }

            public FacadeSpec Unwrap()
            {
                Contract.Assume(Tags != null);
                Contract.Assume(Bottom != null);
                Contract.Assume(Top != null);

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
