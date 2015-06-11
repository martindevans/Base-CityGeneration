
using System;
using System.Collections.Generic;
using System.Linq;
using EpimetheusPlugins.Procedural;
using EpimetheusPlugins.Scripts;

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

        public int Min { get; private set; }
        public int Max { get; private set; }

        public FloorRangeSpec(FloorRangeIncludeSpec[] includes, int min, int max)
        {
            _includes = includes;

            Min = min;
            Max = max;
        }

        public IEnumerable<ScriptReference> Select(Func<double> random, ScriptReference[] verticals, Func<string[], ScriptReference> finder)
        {
            List<ScriptReference[]> selected = new List<ScriptReference[]>();

            foreach (var include in _includes)
                selected.AddRange(include.Select(random, verticals, finder).Select(a => a.ToArray()));

            return selected.SelectMany(a => a);
        }

        internal class Container
            : ISelectorContainer
        {
            public FloorRangeIncludeSpec.Container[] Includes { get; set; }

            public int Min { get; set; }
            public int Max { get; set; }

            public ISelector Unwrap()
            {
                return new FloorRangeSpec(Includes.Select(a => a.Unwrap()).ToArray(), Min, Max);
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
        /// The maximum number of samples to take from this spec to include in the parent range
        /// </summary>
        public int AtMost { get; private set; }

        /// <summary>
        /// The chance (0-100, percentage) of including this spec in the parent range
        /// </summary>
        public float Chance { get; private set; }

        /// <summary>
        /// Whether the samples taken from this range should vary (i.e. if false every sample will be the same)
        /// </summary>
        public bool Vary { get; private set; }

        /// <summary>
        /// Should the items in this include be one continuous run
        /// </summary>
        public bool Continuous { get; private set; }

        public FloorRangeIncludeSpec(int atLeast, int atMost, float chance, bool vary, bool continuous, KeyValuePair<float, string[]>[] tags)
        {
            AtLeast = atLeast;
            AtMost = atMost;
            Chance = chance;
            Vary = vary;
            Continuous = continuous;

            _tags = tags;
        }

        public IEnumerable<IEnumerable<ScriptReference>> Select(Func<double> random, ScriptReference[] verticals, Func<string[], ScriptReference> finder)
        {
            //Skip this range altogether?
            if (Chance < random.RandomSingle(0, 100))
                return new IEnumerable<ScriptReference>[0];

            //How many items to emit?
            var amount = random.RandomInteger(AtLeast, AtMost);

            //Result to emit
            List<List<ScriptReference>> emit = new List<List<ScriptReference>>();

            if (!Vary)
            {
                var node = FloorSpec.SelectSingle(random, _tags, verticals, finder);
                if (Continuous)
                {
                    var l = new List<ScriptReference>();
                    for (int i = 0; i < amount; i++)
                        l.Add(node);
                    emit.Add(l);
                }
                else
                {
                    for (int i = 0; i < amount; i++)
                        emit.Add(new List<ScriptReference> {node});
                }
            }
            else
            {
                if (Continuous)
                {
                    var l = new List<ScriptReference>();
                    for (int i = 0; i < amount; i++)
                        l.Add(FloorSpec.SelectSingle(random, _tags, verticals, finder));
                    emit.Add(l);
                }
                else
                {
                    for (int i = 0; i < amount; i++)
                        emit.Add(new List<ScriptReference> { FloorSpec.SelectSingle(random, _tags, verticals, finder) });
                }
            }

            return emit;

            throw new NotImplementedException();
        }

        internal class Container
        {
            public TagContainer Tags { get; set; }

            public int AtLeast { get; set; }
            public int AtMost { get; set; }
            public float Chance { get; set; }
            public bool Vary { get; set; }
            public bool Continuous { get; set; }

            internal FloorRangeIncludeSpec Unwrap()
            {
                return new FloorRangeIncludeSpec(AtLeast, AtMost, Chance, Vary, Continuous, Tags.ToArray());
            }
        }
    }
}
