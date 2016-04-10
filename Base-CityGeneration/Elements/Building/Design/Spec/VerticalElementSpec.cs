using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Base_CityGeneration.Elements.Building.Design.Spec.Ref;
using Base_CityGeneration.Elements.Building.Internals.VerticalFeatures;
using Base_CityGeneration.Utilities;
using EpimetheusPlugins.Scripts;
using JetBrains.Annotations;

namespace Base_CityGeneration.Elements.Building.Design.Spec
{
    public class VerticalElementSpec
    {
        private readonly KeyValuePair<float, KeyValuePair<string, string>[]>[] _tags;
        public IEnumerable<KeyValuePair<float, KeyValuePair<string, string>[]>> Tags
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<KeyValuePair<float, KeyValuePair<string, string>[]>>>() != null);
                return _tags;
            }
        }

        private readonly BaseRef _bottomFloor;
        public BaseRef BottomFloor
        {
            get
            {
                Contract.Ensures(Contract.Result<BaseRef>() != null);
                return _bottomFloor;
            }
        }

        private readonly BaseRef _topFloor;
        public BaseRef TopFloor
        {
            get
            {
                Contract.Ensures(Contract.Result<BaseRef>() != null);
                return _topFloor;
            }
        }

        public VerticalElementSpec(KeyValuePair<float, KeyValuePair<string, string>[]>[] tags, BaseRef bottomFloor, BaseRef topFloor)
        {
            Contract.Requires(tags != null);
            Contract.Requires(bottomFloor != null);
            Contract.Requires(topFloor != null);

            _tags = tags;
            _topFloor = topFloor;
            _bottomFloor = bottomFloor;
        }

        [ContractInvariantMethod]
        private void ObjectInvariants()
        {
            Contract.Invariant(_bottomFloor != null);
            Contract.Invariant(_topFloor != null);
        }

        public IEnumerable<VerticalSelection> Select(Func<double> random, Func<KeyValuePair<string, string>[], Type[], ScriptReference> finder, FloorSelection[] floors)
        {
            Contract.Requires(random != null);
            Contract.Requires(finder != null);
            Contract.Requires(floors != null);
            Contract.Ensures(Contract.Result<IEnumerable<VerticalSelection>>() != null);

            var bot = BottomFloor.Match(floors, null);
            var zipped = TopFloor.MatchFrom(floors, BottomFloor, bot);

            var output = new List<VerticalSelection>();
            foreach (var vertical in zipped)
            {
                var result = _tags.SelectScript(random, finder, typeof(IVerticalFeature));
                if (result == null)
                    continue;

                output.Add(new VerticalSelection(
                    result.Script,
                    vertical.Key.Index,
                    vertical.Value.Index
                ));
            }
            return output;
        }

        internal class Container
        {
            private TagContainerContainer _tags;
            private BaseRef.BaseContainer _bottom;
            private BaseRef.BaseContainer _top;

            // ReSharper disable once ConvertToAutoPropertyWhenPossible (Backing field needed for CC assumptions below)
            public TagContainerContainer Tags { get { return _tags; } [UsedImplicitly]set { _tags = value; } }

            // ReSharper disable once ConvertToAutoPropertyWhenPossible (Backing field needed for CC assumptions below)
            public BaseRef.BaseContainer Bottom { get { return _bottom; } [UsedImplicitly] set { _bottom = value; } }

            // ReSharper disable once ConvertToAutoPropertyWhenPossible (Backing field needed for CC assumptions below)
            public BaseRef.BaseContainer Top { get { return _top; } [UsedImplicitly] set { _top = value; } }

            public VerticalElementSpec Unwrap()
            {
                Contract.Assume(_tags != null);
                Contract.Assume(_bottom != null);
                Contract.Assume(_top != null);

                return new VerticalElementSpec(
                    _tags.Unwrap().ToArray(),
                    _bottom.Unwrap(),
                    _top.Unwrap()
                );
            }
        }
    }
}
