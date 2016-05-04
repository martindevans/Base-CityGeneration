using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Base_CityGeneration.Elements.Building.Internals.Floors;
using Base_CityGeneration.Utilities;
using Base_CityGeneration.Utilities.Extensions;
using Base_CityGeneration.Utilities.Numbers;
using EpimetheusPlugins.Scripts;
using Myre.Collections;

namespace Base_CityGeneration.Elements.Building.Design.Spec
{
    /// <summary>
    /// Selector for a range of floors
    /// </summary>
    public class FloorRangeSpec
        : BaseFloorSelector
    {
        private readonly FloorRangeIncludeSpec[] _includes;
        public IEnumerable<FloorRangeIncludeSpec> Includes
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<FloorRangeIncludeSpec>>() != null);
                return _includes;
            }
        }

        public override float MinHeight
        {
            get { return _includes.Sum(a => a.Count.MinValue * a.Height.MinValue); }
        }

        public override float MaxHeight
        {
            get { return _includes.Sum(a => a.Count.MaxValue * a.Height.MaxValue); }
        }


        public FloorRangeSpec(FloorRangeIncludeSpec[] includes)
        {
            Contract.Requires(includes != null);

            _includes = includes;
        }

        public override IEnumerable<FloorRun> Select(Func<double> random, INamedDataCollection metadata, Func<KeyValuePair<string, string>[], Type[], ScriptReference> finder)
        {
            var selected = new List<FloorSelection[]>();

            //Includes return an enumerable of enumerables, inner enumerables are items which must be continuous (i.e. all next to each other)
            foreach (var selection in _includes.Select(include => include.Select(this, random, metadata, finder).Select(a => a.Where(b => b.Script != null)).ToArray()))
                selected.AddRange(selection.Select(floorSelection => floorSelection.ToArray()));

            //Shuffle the list, then flatten out the sub lists
            var result = selected
                .Select(data => new {data, r = random()})
                .OrderBy(a => a.r)
                .Select(a => a.data)
                .SelectMany(a => a);

            //A range cannot include footprints, so this is all one run
            yield return new FloorRun(
                result.ToArray(),
                null
            );
        }

        internal class Container
            : ISelectorContainer
        {
            public FloorRangeIncludeSpec.Container[] Includes { get; set; }

            public object DefaultHeight { get; set; }

            public BaseFloorSelector Unwrap()
            {
                IValueGenerator defaultHeight = DefaultHeight == null ? new NormallyDistributedValue(2.5f, 3, 3.5f, 0.2f) : IValueGeneratorContainer.FromObject(DefaultHeight);

                return new FloorRangeSpec(Includes.Select(a => a.Unwrap(defaultHeight)).ToArray());
            }
        }
    }

    public class FloorRangeIncludeSpec
    {
        private readonly KeyValuePair<float, KeyValuePair<string, string>[]>[] _tags;
        /// <summary>
        /// Sets of chances (keyed by relative chance) to satisfy this spec
        /// </summary>
        public IEnumerable<KeyValuePair<float, KeyValuePair<string, string>[]>> Tags
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<KeyValuePair<float, KeyValuePair<string, string>[]>>>() != null);
                return _tags;
            }
        }

        private readonly IValueGenerator _count;
        public IValueGenerator Count
        {
            get
            {
                Contract.Ensures(Contract.Result<IValueGenerator>() != null);
                return _count;
            }
        }

        /// <summary>
        /// Should the items in this include be one continuous run
        /// </summary>
        public bool Continuous { get; private set; }

        public bool Vary { get; private set; }

        private readonly string _id;

        public string Id
        {
            get { return _id; }
        }

        internal IValueGenerator Height;

        public FloorRangeIncludeSpec(string id, IValueGenerator count, bool vary, bool continuous, KeyValuePair<float, KeyValuePair<string, string>[]>[] tags, IValueGenerator height)
        {
            Contract.Requires(count != null);
            Contract.Requires(tags != null);
            Contract.Requires(height != null);

            Continuous = continuous;
            Height = height;
            Vary = vary;

            _id = id;
            _tags = tags;
            _count = count;
        }

        [ContractInvariantMethod]
        private void ObjectInvariants()
        {
            Contract.Invariant(_count != null);
            Contract.Invariant(_tags != null);
        }

        private static FloorSelection SelectSingle(BaseFloorSelector selector, Func<double> random, IEnumerable<KeyValuePair<float, KeyValuePair<string, string>[]>> tags, Func<KeyValuePair<string, string>[], Type[], ScriptReference> finder, float height, string id)
        {
            Contract.Requires(selector != null);
            Contract.Requires(random != null);
            Contract.Requires(tags != null);
            Contract.Requires(finder != null);

            var result = tags.SelectScript(random, finder, typeof(IFloor));
            if (result == null)
                return null;

            return new FloorSelection(id, result.Tags, result.Script, height);
        }

        public IEnumerable<IEnumerable<FloorSelection>> Select(BaseFloorSelector selector, Func<double> random, INamedDataCollection metadata, Func<KeyValuePair<string, string>[], Type[], ScriptReference> finder)
        {
            Contract.Requires(selector != null);
            Contract.Requires(random != null);
            Contract.Requires(metadata != null);
            Contract.Requires(finder != null);
            Contract.Ensures(Contract.Result<IEnumerable<IEnumerable<FloorSelection>>>() != null);

            //How many items to emit?
            var amount = _count.SelectIntValue(random, metadata);

            //Result to emit
            var emit = new List<List<FloorSelection>>();

            //Create a selection function which either always returns the same value or doesn't, depending upon Vary
            Func<FloorSelection> selectFloor;
            if (Vary)
            {
                selectFloor = () => SelectSingle(selector, random, _tags, finder, Height.SelectFloatValue(random, metadata), Id);
            }
            else
            {
                var node = SelectSingle(selector, random, _tags, finder, Height.SelectFloatValue(random, metadata), Id);
                selectFloor = () => node == null ? null : node.Clone();
            }

            if (Continuous)
            {
                var l = new List<FloorSelection>();
                for (int i = 0; i < amount; i++)
                {
                    var f = selectFloor();
                    if (f != null)
                        l.Add(f);
                }
                emit.Add(l);
            }
            else
            {
                for (int i = 0; i < amount; i++)
                {
                    var f = selectFloor();
                    if (f != null)
                        emit.Add(new List<FloorSelection> {
                            f
                        });
                }
            }

            return emit;
        }

        internal class Container
        {
            public TagContainerContainer Tags { get; set; }

            public object Count { get; set; }

            public bool Vary { get; set; }
            public bool Continuous { get; set; }

            public string Id { get; set; }

            public object Height { get; set; }

            internal FloorRangeIncludeSpec Unwrap(IValueGenerator defaultHeight)
            {
                var count = IValueGeneratorContainer.FromObject(Count);

                return new FloorRangeIncludeSpec(Id ?? Guid.NewGuid().ToString(), count, Vary, Continuous, Tags.Unwrap().ToArray(), IValueGeneratorContainer.FromObject(Height, defaultHeight));
            }
        }
    }
}
