using System;
using System.Collections.Generic;
using Base_CityGeneration.Elements.Building.Internals.Floors.Design.Connections;
using Base_CityGeneration.Elements.Building.Internals.Floors.Design.Constraints;
using JetBrains.Annotations;
using Myre.Collections;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Design.Spaces
{
    public abstract class BaseSpaceSpec
        : ISpaceSpecProducer
    {
        public string Id { get; private set; }

        public bool Walkthrough { get; private set; }

        public virtual IReadOnlyList<RequirementStrength<BaseSpaceConstraintSpec>> Constraints { get; private set; }
        public virtual IReadOnlyList<RequirementStrength<BaseSpaceConnectionSpec>> Connections { get; private set; }

        protected BaseSpaceSpec(string id, bool walkthrough, IReadOnlyList<RequirementStrength<BaseSpaceConstraintSpec>> constraints, IReadOnlyList<RequirementStrength<BaseSpaceConnectionSpec>> connections)
        {
            Id = id;
            Walkthrough = walkthrough;

            Constraints = constraints;
            Connections = connections;
        }

        /// <summary>
        /// Get the minimum area this space may occupy
        /// </summary>
        public abstract float MinArea(Func<double> random, INamedDataCollection metadata);

        /// <summary>
        /// Get the minimum area this space may occupy
        /// </summary>
        public abstract float MaxArea(Func<double> random, INamedDataCollection metadata);

        public abstract IEnumerable<BaseSpaceSpec> Produce(bool required, Func<double> random, INamedDataCollection metadata);

        internal abstract class BaseContainer
            : ISpaceSpecProducerContainer
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

            ISpaceSpecProducer IUnwrappable<ISpaceSpecProducer>.Unwrap()
            {
                return Unwrap();
            }
        }
    }
}
