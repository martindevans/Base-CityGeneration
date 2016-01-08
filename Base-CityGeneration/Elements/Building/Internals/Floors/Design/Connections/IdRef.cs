using System;
using JetBrains.Annotations;
using Myre.Collections;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Design.Connections
{
    public class IdRef
        : BaseSpaceConnectionSpec
    {
        private readonly string _id;
        public string Id { get { return _id; } }

        private IdRef(string id)
        {
            _id = id;
        }

        internal class Container
            : BaseContainer
        {
            public string Id { get; [UsedImplicitly]set; }

            public override BaseSpaceConnectionSpec Unwrap(Func<double> random, INamedDataCollection metadata)
            {
                return new IdRef(Id);
            }
        }
    }
}
