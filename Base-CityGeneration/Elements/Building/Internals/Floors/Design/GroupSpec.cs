using System.Collections.Generic;
using System.Linq;
using Base_CityGeneration.Elements.Building.Internals.Floors.Design.Connections;
using Base_CityGeneration.Elements.Building.Internals.Floors.Design.Constraints;
using JetBrains.Annotations;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Design
{
    public class GroupSpec
        : BaseSpaceSpec
    {
        public IReadOnlyList<BaseSpaceSpec> Rooms { get; private set; }

        public GroupSpec(IReadOnlyList<BaseSpaceSpec> rooms, string id, bool walkthrough, IReadOnlyList<RequirementStrength<BaseSpaceConstraintSpec>> constraints, IReadOnlyList<RequirementStrength<BaseSpaceConnectionSpec>> connections)
            : base(id, walkthrough, constraints, connections)
        {
            Rooms = rooms;
        }

        internal class Container
            : BaseContainer
        {
            public BaseContainer[] Rooms { get; [UsedImplicitly]set; }

            internal override BaseSpaceSpec Unwrap()
            {
                return new GroupSpec(
                    Rooms.Select(a => a.Unwrap()).ToArray(),
                    Id,
                    Walkthrough,
                    (Constraints ?? NoConstraints).Select(a => a.Unwrap()).ToArray(),
                    (Connections ?? NoConnections).Select(a => a.Unwrap()).ToArray()
                );
            }
        }
    }
}
