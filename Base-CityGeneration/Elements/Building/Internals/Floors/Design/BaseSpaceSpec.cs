using System.Collections.Generic;
using Base_CityGeneration.Elements.Building.Internals.Floors.Design.Connections;
using Base_CityGeneration.Elements.Building.Internals.Floors.Design.Constraints;
using JetBrains.Annotations;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Design
{
    public abstract class BaseSpaceSpec
    {
        public string Id { get; private set; }

        public bool Walkthrough { get; private set; }

        public IReadOnlyList<RequirementStrength<BaseSpaceConstraintSpec>> Constraints { get; private set; }
        public IReadOnlyList<RequirementStrength<BaseSpaceConnectionSpec>> Connections { get; private set; }

        protected BaseSpaceSpec(string id, bool walkthrough, IReadOnlyList<RequirementStrength<BaseSpaceConstraintSpec>> constraints, IReadOnlyList<RequirementStrength<BaseSpaceConnectionSpec>> connections)
        {
            Id = id;
            Walkthrough = walkthrough;

            Constraints = constraints;
            Connections = connections;
        }

        internal abstract class BaseContainer
        {
            protected static readonly RequirementStrengthContainer<BaseSpaceConstraintSpec, BaseSpaceConstraintSpec.BaseContainer>[] NoConstraints = new RequirementStrengthContainer<BaseSpaceConstraintSpec, BaseSpaceConstraintSpec.BaseContainer>[0];
            protected static readonly RequirementStrengthContainer<BaseSpaceConnectionSpec, BaseSpaceConnectionSpec.BaseContainer>[] NoConnections = new RequirementStrengthContainer<BaseSpaceConnectionSpec, BaseSpaceConnectionSpec.BaseContainer>[0];

            // ReSharper disable MemberCanBeProtected.Global
            public string Id { get; [UsedImplicitly]set; }

            public bool Walkthrough { get; [UsedImplicitly]set; }

            public RequirementStrengthContainer<BaseSpaceConstraintSpec, BaseSpaceConstraintSpec.BaseContainer>[] Constraints { get; [UsedImplicitly]set; }
            public RequirementStrengthContainer<BaseSpaceConnectionSpec, BaseSpaceConnectionSpec.BaseContainer>[] Connections { get; [UsedImplicitly]set; }
            // ReSharper restore MemberCanBeProtected.Global

            internal abstract BaseSpaceSpec Unwrap();
        }
    }
}
