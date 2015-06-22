using System;
using System.Collections.Generic;
using System.Linq;
using Base_CityGeneration.Elements.Building.Internals.Floors.Selection.Spec.Ref;
using EpimetheusPlugins.Procedural;
using EpimetheusPlugins.Scripts;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Selection.Spec
{
    public class VerticalElementSpec
    {
        private readonly KeyValuePair<float, string[]>[] _tags;
        public IEnumerable<KeyValuePair<float, string[]>> Tags
        {
            get
            {
                return _tags;
            }
        }

        public IRef BottomFloor { get; private set; }
        public IRef TopFloor { get; private set; }
        public VerticalElementCreationOptions Create { get; private set; }

        public VerticalElementSpec(KeyValuePair<float, string[]>[] tags, IRef bottomFloor, IRef topFloor, VerticalElementCreationOptions create)
        {
            _tags = tags;
            TopFloor = topFloor;
            Create = create;
            BottomFloor = bottomFloor;
        }

        public IEnumerable<VerticalSelection> Select(Func<double> random, Func<string[], ScriptReference> finder, int basements, FloorSelection[] floors)
        {
            var top = TopFloor.Match(basements, floors);
            var bot = BottomFloor.Match(basements, floors);

            var zipped = bot.Zip(top, (a, b) => new KeyValuePair<FloorSelection, FloorSelection>(a, b));

            var selected = KeyValuePairs(zipped);

            List<VerticalSelection> output = new List<VerticalSelection>();
            foreach (var vertical in selected)
            {
                string[] chosenTags;
                var script = FloorSpec.FindScript(random, finder, _tags, out chosenTags);
                if (script == null)
                    continue;

                output.Add(new VerticalSelection(finder(_tags.WeightedRandom(random)), floors.Length - Array.IndexOf(floors, vertical.Key) - basements - 1, floors.Length - Array.IndexOf(floors, vertical.Value) - basements - 1));
            }
            return output;
        }

        private IEnumerable<KeyValuePair<FloorSelection, FloorSelection>> KeyValuePairs(IEnumerable<KeyValuePair<FloorSelection, FloorSelection>> zipped)
        {
            switch (Create)
            {
                case VerticalElementCreationOptions.All:
                    return zipped;
                case VerticalElementCreationOptions.First:
                    return zipped.Take(1);
                case VerticalElementCreationOptions.Last:
                    return zipped.Reverse().Take(1).ToArray();
                case VerticalElementCreationOptions.Shortest:
                    throw new NotImplementedException();
                case VerticalElementCreationOptions.Longest:
                    throw new NotImplementedException();
                case VerticalElementCreationOptions.SingleOrNone:
                    if (zipped.Skip(1).Any())
                        return new KeyValuePair<FloorSelection, FloorSelection>[0];
                    return zipped.Take(1);
                case VerticalElementCreationOptions.SingleOrFail:
                    throw new NotImplementedException();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        internal class Container
        {
            public TagContainer Tags { get; set; }

            public IRefContainer Bottom { get; set; }
            public IRefContainer Top { get; set; }

            public VerticalElementCreationOptions? Create { get; set; }

            public VerticalElementSpec Unwrap()
            {
                return new VerticalElementSpec(
                    Tags.ToArray(),
                    Bottom.Unwrap(),
                    Top.Unwrap(),
                    Create ?? VerticalElementCreationOptions.All
                );
            }
        }
    }

    public enum VerticalElementCreationOptions
    {
        /// <summary>
        /// When there are multiple choices, take them all
        /// </summary>
        All,

        /// <summary>
        /// Take the first created element
        /// </summary>
        First,

        /// <summary>
        /// Take the last created element
        /// </summary>
        Last,

        /// <summary>
        /// Take the shortest element
        /// </summary>
        Shortest,

        /// <summary>
        /// Take the longest element
        /// </summary>
        Longest,

        /// <summary>
        /// If only one option was generated, take that. Otherwise take none
        /// </summary>
        SingleOrNone,

        /// <summary>
        /// If only one option was generated, take that. Otherwise take fail (cancel entire building)
        /// </summary>
        SingleOrFail,
    }
}
