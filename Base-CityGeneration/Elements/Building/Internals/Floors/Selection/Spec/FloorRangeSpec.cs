
using System;
using System.Collections.Generic;
using System.Linq;
using EpimetheusPlugins.Procedural;
using EpimetheusPlugins.Scripts;
using Microsoft.Xna.Framework;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Selection.Spec
{
    /// <summary>
    /// Selector for a range of floors
    /// </summary>
    public class FloorRangeSpec
        : ISelector
    {
        private readonly FloorRangeIncludeSpec[] _includes;
        public IEnumerable<FloorRangeIncludeSpec> Includes
        {
            get
            {
                return _includes;
            }
        }

        private readonly HeightSpec _defaultHeightSpec;
        public HeightSpec DefaultHeight
        {
            get
            {
                return _defaultHeightSpec;
            }
        }

        public FloorRangeSpec(FloorRangeIncludeSpec[] includes, HeightSpec defaultHeightSpec)
        {
            _includes = includes;
            _defaultHeightSpec = defaultHeightSpec;

            foreach (var include in includes)
                include.Height = include.Height ?? defaultHeightSpec;
        }

        public IEnumerable<FloorSelection> Select(Func<double> random, ScriptReference[] verticals, Func<string[], ScriptReference> finder, IGroupFinder groupFinder)
        {
            List<FloorSelection[]> selected = new List<FloorSelection[]>();

            //Includes return an enuemrable of enumerables, inner enumerables are items which must be continuous (i.e. all next to each other)
            foreach (var include in _includes)
            {
                var selection = include
                    .Select(random, verticals, finder, groupFinder)
                    .Select(a => a.Where(b => b.Script != null))
                    .ToArray();

                foreach (var floorSelection in selection)
                    selected.Add(floorSelection.ToArray());
            }

            //Shuffle the list, then flatten out the sub lists
            return selected
                .Select(data => new {data, r = random()}).OrderBy(a => a.r).Select(a => a.data)
                .SelectMany(a => a);
        }

        internal class Container
            : ISelectorContainer
        {
            public FloorRangeIncludeSpec.Container[] Includes { get; set; }

            public HeightSpec.Container DefaultHeight { get; set; }

            public ISelector Unwrap()
            {
                var defaultHeight = DefaultHeight.Unwrap();

                return new FloorRangeSpec(Includes.Select(a => a.Unwrap(defaultHeight)).ToArray(), defaultHeight);
            }
        }
    }

    public class FloorRangeIncludeSpec
    {
        private readonly KeyValuePair<float, string[]>[] _tags;

        /// <summary>
        /// Sets of chances (keyed by relative chance) to satisfy this spec
        /// </summary>
        public IEnumerable<KeyValuePair<float, string[]>> Tags
        {
            get
            {
                return _tags;
            }
        }

        /// <summary>
        /// The minimum number of samples to take from this spec to include in the parent range
        /// </summary>
        public int AtLeast { get; private set; }

        /// <summary>
        /// The mean number of samples to take from this spec to include in the parent range
        /// </summary>
        public float Mean { get; private set; }

        /// <summary>
        /// The maximum number of samples to take from this spec to include in the parent range
        /// </summary>
        public int AtMost { get; private set; }

        /// <summary>
        /// The standard deviation to use when selecting the number of items to take from this spec
        /// </summary>
        public float Deviation { get; private set; }

        /// <summary>
        /// Whether the samples taken from this range should vary (i.e. if false every sample will be the same)
        /// </summary>
        public bool Vary { get; private set; }

        /// <summary>
        /// Should the items in this include be one continuous run
        /// </summary>
        public bool Continuous { get; private set; }

        internal HeightSpec Height;

        public FloorRangeIncludeSpec(int atLeast, float mean, int atMost, float stdDeviation, bool vary, bool continuous, KeyValuePair<float, string[]>[] tags, HeightSpec height)
        {
            AtLeast = atLeast;
            Mean = mean;
            AtMost = atMost;
            Deviation = stdDeviation;
            Vary = vary;
            Continuous = continuous;

            Height = height;

            _tags = tags;
        }

        public IEnumerable<IEnumerable<FloorSelection>> Select(Func<double> random, ScriptReference[] verticals, Func<string[], ScriptReference> finder, IGroupFinder groupFinder)
        {
            //How many items to emit?
            int amount = (int)MathHelper.Clamp((float)Math.Round(random.NormallyDistributedSingle(Deviation, Mean), MidpointRounding.AwayFromZero), AtLeast, AtMost);

            //Result to emit
            List<List<FloorSelection>> emit = new List<List<FloorSelection>>();

            //Create a selection function which either always returns the same value or doesn't, depending upon Vary
            Func<ScriptReference> selectScript;
            if (Vary)
                selectScript = () => FloorSpec.SelectSingle(random, _tags, verticals, finder);
            else
            {
                var node = FloorSpec.SelectSingle(random, _tags, verticals, finder);
                selectScript = () => node;
            }

            Func<FloorSelection> selectFloor = () => new FloorSelection(selectScript(), Height.SelectHeight(random, groupFinder));

            if (Continuous)
            {
                var l = new List<FloorSelection>();
                for (int i = 0; i < amount; i++)
                    l.Add(selectFloor());
                emit.Add(l);
            }
            else
            {
                for (int i = 0; i < amount; i++)
                    emit.Add(new List<FloorSelection> { selectFloor() });
            }

            return emit;
        }

        internal class Container
        {
            public TagContainer Tags { get; set; }

            public int? AtLeast { get; set; }
            public float? Mean { get; set; }
            public int? AtMost { get; set; }

            public float? Deviation { get; set; }

            public bool Vary { get; set; }
            public bool Continuous { get; set; }

            internal FloorRangeIncludeSpec Unwrap(HeightSpec defaultHeight)
            {
                var atLeast = AtLeast ?? 1;
                var atMost = AtMost ?? 1;

                var mean = Mean ?? (atLeast * 0.5f + atMost * 0.5f);
                var deviation = Deviation ?? (atMost - atLeast) * 0.2f;

                return new FloorRangeIncludeSpec(atLeast, mean, atMost, deviation, Vary, Continuous, Tags.ToArray(), defaultHeight);
            }
        }
    }
}
