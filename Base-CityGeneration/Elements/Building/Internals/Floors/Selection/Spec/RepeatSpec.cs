using Base_CityGeneration.Utilities.Numbers;
using EpimetheusPlugins.Scripts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Selection.Spec
{
    public class RepeatSpec
        : ISelector
    {
        public ISelector[] Items { get; private set; }
        public IValueGenerator Count { get; private set; }

        public bool Vary { get; private set; }

        private RepeatSpec(ISelector[] items, IValueGenerator count, bool vary)
        {
            Items = items;
            Count = count;
            Vary = vary;
        }

        public IEnumerable<FloorSelection> Select(Func<double> random, Func<string[], ScriptReference> finder)
        {
            int count = Count.SelectIntValue(random);

            List<FloorSelection> selection = new List<FloorSelection>();
            if (Vary)
            {
                for (int i = 0; i < count; i++)
                    foreach (var selector in Items)
                        selection.AddRange(selector.Select(random, finder));
            }
            else
            {
                //Generate selections for each item in the repeat (cached)
                List<FloorSelection[]> selectionCache = Items.Select(selector => selector.Select(random, finder).ToArray()).ToList();

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

            public IValueGeneratorContainer Count { get; set; }

            public bool Vary { get; set; }

            public ISelector Unwrap()
            {
                return new RepeatSpec(
                    Items.Select(a => a.Unwrap()).ToArray(),
                    Count.Unwrap(),
                    Vary
                );
            }
        }
    }
}
