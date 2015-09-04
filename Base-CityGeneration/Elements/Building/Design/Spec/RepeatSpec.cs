using System;
using System.Collections.Generic;
using System.Linq;
using Base_CityGeneration.Utilities.Numbers;
using EpimetheusPlugins.Scripts;
using Myre.Collections;

namespace Base_CityGeneration.Elements.Building.Design.Spec
{
    public class RepeatSpec
        : IFloorSelector
    {
        public IFloorSelector[] Items { get; private set; }
        public IValueGenerator Count { get; private set; }

        public bool Vary { get; private set; }

        private RepeatSpec(IFloorSelector[] items, IValueGenerator count, bool vary)
        {
            Items = items;
            Count = count;
            Vary = vary;
        }

        public IEnumerable<FloorSelection> Select(Func<double> random, INamedDataCollection metadata, Func<string[], ScriptReference> finder)
        {
            int count = Count.SelectIntValue(random, metadata);

            List<FloorSelection> selection = new List<FloorSelection>();
            if (Vary)
            {
                for (int i = 0; i < count; i++)
                    foreach (var selector in Items)
                        selection.AddRange(selector.Select(random, metadata, finder));
            }
            else
            {
                //Generate selections for each item in the repeat (cached)
                List<FloorSelection[]> selectionCache = Items.Select(selector => selector.Select(random, metadata, finder).ToArray()).ToList();

                //Now repeat those cached items as many times as we need
                for (int i = 0; i < count; i++)
                    foreach (var cache in selectionCache)
                        selection.AddRange(cache);
            }
            return selection;
        }

        internal class Container
            : ISelectorContainer
        {
            public ISelectorContainer[] Items { get; set; }

            public object Count { get; set; }

            public bool Vary { get; set; }

            public IFloorSelector Unwrap()
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
