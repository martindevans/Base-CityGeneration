using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Base_CityGeneration.Elements.Building.Internals.Floors.Design.Connections;
using Base_CityGeneration.Elements.Building.Internals.Floors.Design.Constraints;
using JetBrains.Annotations;
using Myre.Collections;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Design.Spaces
{
    public class GroupSpec
        : BaseSpaceSpec
    {
        public IReadOnlyList<BaseSpaceSpec> Rooms { get; private set; }

        public GroupSpec(IReadOnlyList<BaseSpaceSpec> rooms, string id, bool walkthrough, bool verticalAttach, IReadOnlyList<RequirementStrength<BaseSpaceConstraintSpec>> constraints, IReadOnlyList<RequirementStrength<BaseSpaceConnectionSpec>> connections)
            : base(id, walkthrough, verticalAttach, constraints, connections)
        {
            Rooms = rooms;
        }

        public override float MinArea()
        {
            return Rooms.Select(r => r.MinArea()).Sum();
        }

        public override float MaxArea()
        {
            return Rooms.Select(r => r.MaxArea()).Sum();
        }

        public override IReadOnlyList<RequirementStrength<BaseSpaceConstraintSpec>> Constraints
        {
            //We override the constraints property and return the union of all the constraints of the child rooms
            get
            {
                return (from room in Rooms
                        from constraint in room.Constraints
                        group constraint by constraint.Requirement.GetType()
                        into grouped
                        select grouped.Aggregate(Union)
                ).ToArray();
            }
        }

        public override IEnumerable<BaseSpaceSpec> Produce(bool required, Func<double> random, INamedDataCollection metadata)
        {
            yield return this;
        }

        private static RequirementStrength<T> Union<T>(RequirementStrength<T> a, RequirementStrength<T> b)
            where T : BaseSpaceConstraintSpec
        {
            return new RequirementStrength<T>(
                a.Requirement.Union<T>(b.Requirement),
                a.Strength * 0.5f + b.Strength * 0.5f
            );
        }

        internal class Container
            : BaseContainer
        {
            public BaseContainer[] Rooms { get; [UsedImplicitly]set; }

            protected internal override BaseSpaceSpec Unwrap(Func<double> random, INamedDataCollection metadata)
            {
                Contract.Assume(Rooms != null);

                return new GroupSpec(
                    Rooms.Select(a => a.Unwrap(random, metadata)).ToArray(),
                    Id,
                    Walkthrough,
                    VerticalAttach,
                    (Constraints ?? NoConstraints).Select(a => a.Unwrap(random, metadata)).ToArray(),
                    (Connections ?? NoConnections).Select(a => a.Unwrap(random, metadata)).ToArray()
                );
            }
        }
    }
}
