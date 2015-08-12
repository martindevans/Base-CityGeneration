using System;
using System.Collections.Generic;
using System.Linq;
using Base_CityGeneration.Elements.Building.Internals.Floors.Selection.Spec.Ref;
using Base_CityGeneration.Utilities;
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
        
        public VerticalElementSpec(KeyValuePair<float, string[]>[] tags, IRef bottomFloor, IRef topFloor)
        {
            _tags = tags;
            TopFloor = topFloor;
            BottomFloor = bottomFloor;
        }

        public IEnumerable<VerticalSelection> Select(Func<double> random, Func<string[], ScriptReference> finder, int basements, FloorSelection[] floors)
        {
            var bot = BottomFloor.Match(basements, floors);

            var zipped = FilterByCreationMode(bot.SelectMany(a =>
                FilterByCreationMode(TopFloor
                    .Match(basements, floors, Array.IndexOf(floors, a))
                    .Select(b => new KeyValuePair<FloorSelection, FloorSelection>(a, b))
                    .Where(b => b.Key.Index != b.Value.Index), TopFloor.Filter)
            ), BottomFloor.Filter);

            List<VerticalSelection> output = new List<VerticalSelection>();
            foreach (var vertical in zipped)
            {
                string[] chosenTags;
                var script = _tags.SelectScript(random, finder, out chosenTags);
                if (script == null)
                    continue;

                output.Add(new VerticalSelection(finder(_tags.WeightedRandom(random)), floors.Length - Array.IndexOf(floors, vertical.Key) - basements - 1, floors.Length - Array.IndexOf(floors, vertical.Value) - basements - 1));
            }
            return output;
        }

        private IEnumerable<KeyValuePair<FloorSelection, FloorSelection>> FilterByCreationMode(IEnumerable<KeyValuePair<FloorSelection, FloorSelection>> zipped, VerticalElementCreationOptions option)
        {
            switch (option)
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

            public VerticalElementSpec Unwrap()
            {
                return new VerticalElementSpec(
                    Tags.ToArray(),
                    Bottom.Unwrap(),
                    Top.Unwrap()
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
