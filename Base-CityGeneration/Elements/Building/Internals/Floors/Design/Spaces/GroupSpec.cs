using System;
using System.Collections.Generic;
using System.Linq;
using Base_CityGeneration.Elements.Building.Internals.Floors.Design.Spaces.Constraints;
using Base_CityGeneration.Elements.Building.Internals.Floors.Design.Spaces.Selector;
using JetBrains.Annotations;
using Myre.Collections;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Design.Spaces
{
    /// <summary>
    /// A group is laid out as if it were a room, and then is expanded and runs it's own floorplan generation in itself
    /// </summary>
    public class GroupSpec
        : BaseSpaceSpec, IProviderSpec, ISpaceSpec
    {
        private readonly IReadOnlyList<ISpec> _specs;

        public bool Walkthrough { get; private set; }

        public bool EntrySpace { get; private set; }

        public IEnumerable<Weighted<BaseConstraint>> Constraints { get; private set; }

        public IEnumerable<Weighted<BaseSpecSelector>> Connections { get; private set; }

        public GroupSpec(string id, bool walkthrough, bool entrySpace, IReadOnlyList<Weighted<BaseConstraint>> constraints, IReadOnlyList<Weighted<BaseSpecSelector>> connections, IReadOnlyList<ISpec> specs)
            : base(id)
        {
            _specs = specs;

            Walkthrough = walkthrough;
            EntrySpace = entrySpace;
            Constraints = constraints;
            Connections = connections;
        }

        public IEnumerable<ISpec> Expand(Func<double> random, INamedDataCollection metadata)
        {
            return _specs;
        }

        internal class Container
            : BaseContainer
        {
            // ReSharper disable CollectionNeverUpdated.Global
            // ReSharper disable MemberCanBePrivate.Global
            public List<Weighted<BaseConstraint>.Container<BaseConstraint.BaseContainer>> Constraints { get; [UsedImplicitly] set; }
            public List<Weighted<BaseSpecSelector>.Container<BaseSpecSelector.BaseContainer>> Connections { get; [UsedImplicitly] set; }

            public bool Walkthrough { get; [UsedImplicitly]set; }
            public bool EntrySpace { get; [UsedImplicitly]set; }

            public List<BaseContainer> Children { get; [UsedImplicitly]set; }
            // ReSharper restore MemberCanBePrivate.Global
            // ReSharper restore CollectionNeverUpdated.Global

            public override BaseSpaceSpec Unwrap()
            {
                return new GroupSpec(
                    Id,
                    Walkthrough,
                    EntrySpace,
                    Constraints.UnwrapEnumerable().ToArray(),
                    Connections.UnwrapEnumerable().ToArray(),
                    Children.Select(a => a.Unwrap()).ToArray()
                );
            }
        }
    }
}
