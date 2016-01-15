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
        /// <summary>
        /// ID of this floor, used for debugging and error message purposes
        /// </summary>
        public string Id { get; private set; }

        /// <summary>
        /// Indicates if this space may be used to connect to other spaces (i.e. people may walk through this space to get to the spaces)
        /// </summary>
        public bool Walkthrough { get; private set; }

        /// <summary>
        /// Indicates if vertical elements may be attached directly to this space (e.g. some kind of skylobby)
        /// </summary>
        public bool VerticalAttach { get; private set; }

        public virtual IReadOnlyList<RequirementStrength<BaseSpaceConstraintSpec>> Constraints { get; private set; }
        public virtual IReadOnlyList<RequirementStrength<BaseSpaceConnectionSpec>> Connections { get; private set; }

        protected BaseSpaceSpec(string id, bool walkthrough, bool verticalAttach, IReadOnlyList<RequirementStrength<BaseSpaceConstraintSpec>> constraints, IReadOnlyList<RequirementStrength<BaseSpaceConnectionSpec>> connections)
        {
            Id = id;
            Walkthrough = walkthrough;

            Constraints = constraints;
            Connections = connections;
        }

        /// <summary>
        /// Get the minimum area this space may occupy
        /// </summary>
        public abstract float MinArea();

        /// <summary>
        /// Get the minimum area this space may occupy
        /// </summary>
        public abstract float MaxArea();

        public abstract IEnumerable<BaseSpaceSpec> Produce(bool required, Func<double> random, INamedDataCollection metadata);

        internal abstract class BaseContainer
            : ISpaceSpecProducerContainer
        {
            protected static readonly RequirementStrengthContainer<BaseSpaceConstraintSpec, BaseSpaceConstraintSpec.BaseContainer>[] NoConstraints = new RequirementStrengthContainer<BaseSpaceConstraintSpec, BaseSpaceConstraintSpec.BaseContainer>[0];
            protected static readonly RequirementStrengthContainer<BaseSpaceConnectionSpec, BaseSpaceConnectionSpec.BaseContainer>[] NoConnections = new RequirementStrengthContainer<BaseSpaceConnectionSpec, BaseSpaceConnectionSpec.BaseContainer>[0];

            // ReSharper disable MemberCanBeProtected.Global
            public string Id { get; [UsedImplicitly]set; }

            public bool Walkthrough { get; [UsedImplicitly]set; }
            public bool VerticalAttach { get; [UsedImplicitly]set; }

            public object WallThickness { get; [UsedImplicitly]set; }

            public RequirementStrengthContainer<BaseSpaceConstraintSpec, BaseSpaceConstraintSpec.BaseContainer>[] Constraints { get; [UsedImplicitly]set; }
            public RequirementStrengthContainer<BaseSpaceConnectionSpec, BaseSpaceConnectionSpec.BaseContainer>[] Connections { get; [UsedImplicitly]set; }
            // ReSharper restore MemberCanBeProtected.Global

            protected internal abstract BaseSpaceSpec Unwrap(Func<double> random, INamedDataCollection metadata);

            ISpaceSpecProducer IUnwrappable2<ISpaceSpecProducer>.Unwrap(Func<double> random, INamedDataCollection metadata)
            {
                return Unwrap(random, metadata);
            }
        }
    }
}
