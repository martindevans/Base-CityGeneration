using System;
using System.Collections.Generic;
using System.Linq;
using Base_CityGeneration.Elements.Building.Design.Spec.Ref;
using Base_CityGeneration.Utilities;
using EpimetheusPlugins.Procedural;
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
                return _tags;
            }
        }

        public BaseRef BottomFloor { get; private set; }
        public BaseRef TopFloor { get; private set; }

        public VerticalElementSpec(KeyValuePair<float, KeyValuePair<string, string>[]>[] tags, BaseRef bottomFloor, BaseRef topFloor)
        {
            _tags = tags;
            TopFloor = topFloor;
            BottomFloor = bottomFloor;
        }

        public IEnumerable<VerticalSelection> Select(Func<double> random, Func<IEnumerable<KeyValuePair<string, string>>, ScriptReference> finder, FloorSelection[] floors)
        {
            var bot = BottomFloor.Match(floors, null);
            var zipped = TopFloor.MatchFrom(floors, BottomFloor, bot);

            List<VerticalSelection> output = new List<VerticalSelection>();
            foreach (var vertical in zipped)
            {
                KeyValuePair<string, string>[] chosenTags;
                var script = _tags.SelectScript(random, finder, out chosenTags);
                if (script == null)
                    continue;

                output.Add(new VerticalSelection(
                    finder(_tags.WeightedRandom(random)),
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
