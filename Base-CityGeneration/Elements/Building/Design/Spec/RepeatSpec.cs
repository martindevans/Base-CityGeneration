using System;
using System.Collections.Generic;
using System.Linq;
using Base_CityGeneration.Utilities.Numbers;
using EpimetheusPlugins.Scripts;
using Myre.Collections;

namespace Base_CityGeneration.Elements.Building.Design.Spec
{
    public class RepeatSpec
        : BaseFloorSelector
    {
        public BaseFloorSelector[] Items { get; private set; }
        public IValueGenerator Count { get; private set; }

        public override float MinHeight
        {
            get { return Items.Sum(a => a.MinHeight) * Count.MinValue; }
        }

        public override float MaxHeight
        {
            get { return Items.Sum(a => a.MaxHeight) * Count.MaxValue; }
        }

        public bool Vary { get; private set; }

        private RepeatSpec(BaseFloorSelector[] items, IValueGenerator count, bool vary)
        {
            Items = items;
            Count = count;
            Vary = vary;
        }

        public override IEnumerable<FloorRun> Select(Func<double> random, INamedDataCollection metadata, Func<string[], ScriptReference> finder)
        {
            int count = Count.SelectIntValue(random, metadata);

            if (Vary)
            {
                //Run the selectors multiple times, each independent of one another
                return from i in Enumerable.Range(0, count)
                       from selector in Items
                       from run in selector.Select(random, metadata, finder)
                       select run;
            }
            else
            {
                //Generate selections for each item in the repeat (cached)
                var selectionCache = Items.Select(selector => selector.Select(random, metadata, finder).ToArray()).ToList();

                //Repeat the same runs over multiple times
                return from i in Enumerable.Range(0, count)
                       from cache in selectionCache
                       from run in cache
                       select run.Clone();
            }
        }

        internal class Container
            : ISelectorContainer
        {
            public ISelectorContainer[] Items { get; set; }

            public object Count { get; set; }

            public bool Vary { get; set; }

            public BaseFloorSelector Unwrap()
            {
                return new RepeatSpec(
                    Items.Select(a => a.Unwrap()).ToArray(),
                    BaseValueGeneratorContainer.FromObject(Count),
                    Vary
                );
            }
        }
    }
}
