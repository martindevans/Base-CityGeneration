using System;
using System.Collections.Generic;
using System.Linq;
using EpimetheusPlugins.Scripts;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Selection.Spec
{
    public class RepeatSpec
        : ISelector
    {
        public ISelector[] Items { get; private set; }
        public NormalValueSpec Count { get; private set; }

        public bool Vary { get; private set; }

        private RepeatSpec(ISelector[] items, NormalValueSpec count, bool vary)
        {
            Items = items;
            Count = count;
            Vary = vary;
        }

        public IEnumerable<FloorSelection> Select(Func<double> random, ScriptReference[] verticals, Func<string[], ScriptReference> finder, IGroupFinder groupFinder)
        {
            int count = Count.SelectIntValue(random, groupFinder);

            List<FloorSelection> selection = new List<FloorSelection>();
            if (Vary)
            {
                for (int i = 0; i < count; i++)
                    foreach (var selector in Items)
                        selection.AddRange(selector.Select(random, verticals, finder, groupFinder));
            }
            else
            {
                List<FloorSelection[]> selectionCache = new List<FloorSelection[]>();

                //Generate selections for each item in the repeat (cached)
                foreach (var selector in Items)
                    selectionCache.Add(selector.Select(random, verticals, finder, groupFinder).ToArray());

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

            public NormalValueSpec.Container Count { get; set; }

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
