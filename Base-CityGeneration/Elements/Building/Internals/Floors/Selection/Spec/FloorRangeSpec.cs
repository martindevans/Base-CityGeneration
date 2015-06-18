using System;
using System.Collections.Generic;
using System.Linq;
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

        private readonly NormalValueSpec _defaultHeightSpec;
        public NormalValueSpec DefaultHeight
        {
            get
            {
                return _defaultHeightSpec;
            }
        }

        public FloorRangeSpec(FloorRangeIncludeSpec[] includes, NormalValueSpec defaultHeightSpec)
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

            public NormalValueSpec.Container DefaultHeight { get; set; }

            public ISelector Unwrap()
            {
                NormalValueSpec defaultHeight;
                if (DefaultHeight == null)
                    defaultHeight = new NormalValueSpec(2.5f, 3, 3.5f, 0.2f);
                else
                    defaultHeight  = DefaultHeight.Unwrap();

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

        private readonly NormalValueSpec _count;
        public NormalValueSpec Count
        {
            get
            {
                return _count;
            }
        }

        /// <summary>
        /// Should the items in this include be one continuous run
        /// </summary>
        public bool Continuous { get; private set; }

        public bool Vary { get; private set; }

        internal NormalValueSpec Height;

        public FloorRangeIncludeSpec(NormalValueSpec count, bool vary, bool continuous, KeyValuePair<float, string[]>[] tags, NormalValueSpec height)
        {
            Continuous = continuous;
            Height = height;
            Vary = vary;

            _tags = tags;
            _count = count;
        }

        public IEnumerable<IEnumerable<FloorSelection>> Select(Func<double> random, ScriptReference[] verticals, Func<string[], ScriptReference> finder, IGroupFinder groupFinder)
        {
            //How many items to emit?
            int amount = _count.SelectIntValue(random, groupFinder);

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

            Func<FloorSelection> selectFloor = () => new FloorSelection(selectScript(), Height.SelectFloatValue(random, groupFinder));

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

            public NormalValueSpec.Container Count { get; set; }

            public bool Vary { get; set; }
            public bool Continuous { get; set; }

            internal FloorRangeIncludeSpec Unwrap(NormalValueSpec defaultHeight)
            {
                var count = Count.Unwrap();

                return new FloorRangeIncludeSpec(count, Vary, Continuous, Tags.ToArray(), defaultHeight);
            }
        }
    }
}
