using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Base_CityGeneration.Elements.Building.Design.Spec.Ref;
using Base_CityGeneration.Elements.Building.Internals.VerticalFeatures;
using Base_CityGeneration.Utilities;
using EpimetheusPlugins.Scripts;

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
                KeyValuePair<string, string>[] chosenTags;
                var script = _tags.SelectScript(random, finder, out chosenTags, typeof(IVerticalFeature));
                if (script == null)
                    continue;

                output.Add(new VerticalSelection(
                    script,
                    vertical.Key.Index,
                    vertical.Value.Index
                ));
            }
            return output;
        }

        internal class Container
        {
            public TagContainerContainer Tags { get; set; }

            public BaseRef.BaseContainer Bottom { get; set; }
            public BaseRef.BaseContainer Top { get; set; }

            public VerticalElementSpec Unwrap()
            {
                return new VerticalElementSpec(
                    Tags.Unwrap().ToArray(),
                    Bottom.Unwrap(),
                    Top.Unwrap()
                );
            }
        }
    }
}
