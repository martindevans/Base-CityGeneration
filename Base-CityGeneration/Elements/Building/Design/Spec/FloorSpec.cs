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
        : BaseFloorSelector
    {
        public override float MinHeight
        {
            get { return _height.MinValue; }
        }

        public override float MaxHeight
        {
            get { return _height.MaxValue; }
        }

        private readonly KeyValuePair<float, KeyValuePair<string, string>[]>[] _tags;
        public IEnumerable<KeyValuePair<float, KeyValuePair<string, string>[]>> Tags
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

        public FloorSpec(KeyValuePair<float, KeyValuePair<string, string>[]>[] tags, IValueGenerator height)
            : this(Guid.NewGuid().ToString(), tags, height)
        {
        }

        public FloorSpec(string id, KeyValuePair<float, KeyValuePair<string, string>[]>[] tags, IValueGenerator height)
        {
            _id = id;
            _tags = tags;
            _height = height;
        }

        public override IEnumerable<FloorRun> Select(Func<double> random, INamedDataCollection metadata, Func<KeyValuePair<string, string>[], Type[], ScriptReference> finder)
        {
            var selected = SelectSingle(random, _tags, finder, _height.SelectFloatValue(random, metadata), Id);
            if (selected == null)
                yield return new FloorRun(new FloorSelection[0], null);
            else
                yield return new FloorRun(new FloorSelection[] { selected }, null);
        }

        internal class Container
            : ISelectorContainer
        {
            public string Id { get; set; }

            public TagContainerContainer Tags { get; set; }

            public object Height { get; set; }

            public FacadeSpec.Container[] Facades { get; set; }

            public BaseFloorSelector Unwrap()
            {
                IValueGenerator height = Height == null ? new NormallyDistributedValue(2.5f, 3f, 3.5f, 0.2f) : BaseValueGeneratorContainer.FromObject(Height);

                return new FloorSpec(Id ?? Guid.NewGuid().ToString(), Tags.Unwrap().ToArray(), height);
            }
        }
    }
}
