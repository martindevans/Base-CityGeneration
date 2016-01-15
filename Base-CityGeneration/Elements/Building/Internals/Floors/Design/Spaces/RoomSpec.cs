using System;
using System.Collections.Generic;
using System.Linq;
using Base_CityGeneration.Elements.Building.Internals.Floors.Design.Connections;
using Base_CityGeneration.Elements.Building.Internals.Floors.Design.Constraints;
using Base_CityGeneration.Utilities;
using JetBrains.Annotations;
using Myre.Collections;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Design.Spaces
{
    public class RoomSpec
        : BaseSpaceSpec
    {
        public bool Optional { get; private set; }

        public IReadOnlyList<KeyValuePair<float, KeyValuePair<string, string>[]>> Tags { get; private set; }

        public RoomSpec(IReadOnlyList<KeyValuePair<float, KeyValuePair<string, string>[]>> tags, string id, bool walkthrough, bool verticalAttach, IReadOnlyList<RequirementStrength<BaseSpaceConstraintSpec>> constraints, IReadOnlyList<RequirementStrength<BaseSpaceConnectionSpec>> connections, bool optional)
            : base(id, walkthrough, verticalAttach, constraints, connections)
        {
            Tags = tags;
            Optional = optional;
        }

        public override float MinArea()
        {
            return ((Area)Constraints.Single(a => a.Requirement is Area).Requirement).Minimum;
        }

        public override float MaxArea()
        {
            return ((Area)Constraints.Single(a => a.Requirement is Area).Requirement).Maximum;
        }

        public override IEnumerable<BaseSpaceSpec> Produce(bool required, Func<double> random, INamedDataCollection metadata)
        {
            if (Optional != required)
                yield return this;
        }

        internal class Container
            : BaseContainer
        {
            // ReSharper disable once CollectionNeverUpdated.Global
            public TagContainerContainer Tags { get; [UsedImplicitly] set; }

            public bool Optional { get; [UsedImplicitly] private set; }

            protected internal override BaseSpaceSpec Unwrap(Func<double> random, INamedDataCollection metadata)
            {
                if (!Constraints.Any(a => a.Req is Area.Container))
                    throw new SharpYaml.SemanticErrorException(string.Format("Room spec \"{0}\" must specify an Area constraint", Id));

                return new RoomSpec(
                    Tags.Unwrap().ToArray(),
                    Id,
                    Walkthrough,
                    VerticalAttach,
                    (Constraints ?? NoConstraints).Select(a => a.Unwrap(random, metadata)).ToArray(),
                    (Connections ?? NoConnections).Select(a => a.Unwrap(random, metadata)).ToArray(),
                    Optional
                );
            }
        }
    }
}
