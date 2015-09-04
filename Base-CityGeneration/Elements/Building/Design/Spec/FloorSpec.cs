using System;
using System.Collections.Generic;
using System.Linq;
using Base_CityGeneration.Utilities;
using Base_CityGeneration.Utilities.Numbers;
using EpimetheusPlugins.Scripts;
using Myre.Collections;

namespace Base_CityGeneration.Elements.Building.Design.Spec
{
    /// <summary>
    /// Selector for a single floor
    /// </summary>
    public class FloorSpec
        : IFloorSelector
    {
        private readonly KeyValuePair<float, string[]>[] _tags;
        public IEnumerable<KeyValuePair<float, string[]>> Tags
        {
            get
            {
                return _tags;
            }
        }

        private readonly IValueGenerator _height;
        public IValueGenerator Height
        {
            get
            {
                return _height;
            }
        }

        private readonly string _id;
        public string Id { get { return _id; } }

        public FloorSpec(KeyValuePair<float, string[]>[] tags, IValueGenerator height)
            : this(Guid.NewGuid().ToString(), tags, height)
        {
        }

        public FloorSpec(string id, KeyValuePair<float, string[]>[] tags, IValueGenerator height)
        {
            _id = id;
            _tags = tags;
            _height = height;
        }

        public IEnumerable<FloorSelection> Select(Func<double> random, INamedDataCollection metadata, Func<string[], ScriptReference> finder)
        {
            var selected = SelectSingle(random, _tags, finder, _height.SelectFloatValue(random, metadata), Id);
            if (selected == null)
                return new FloorSelection[0];

            return new FloorSelection[] { selected };
        }

        public static FloorSelection SelectSingle(Func<double> random, IEnumerable<KeyValuePair<float, string[]>> tags, Func<string[], ScriptReference> finder, float height, string id)
        {
            string[] selectedTags;
            ScriptReference script = tags.SelectScript(random, finder, out selectedTags);
            if (script == null)
                return null;

            return new FloorSelection(id, selectedTags, script, height);
        }

        internal class Container
            : ISelectorContainer
        {
            public string Id { get; set; }

            public TagContainer Tags { get; set; }

            public object Height { get; set; }

            public FacadeSpec.Container[] Facades { get; set; }

            public IFloorSelector Unwrap()
            {
                IValueGenerator height = Height == null ? new NormallyDistributedValue(2.5f, 3f, 3.5f, 0.2f) : BaseValueGeneratorContainer.FromObject(Height);

                return new FloorSpec(Id ?? Guid.NewGuid().ToString(), Tags.ToArray(), height);
            }
        }
    }
}
