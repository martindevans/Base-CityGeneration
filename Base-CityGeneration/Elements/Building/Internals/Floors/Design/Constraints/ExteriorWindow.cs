using System;
using System.Diagnostics.Contracts;
using System.Linq;
using JetBrains.Annotations;
using Myre.Collections;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Design.Constraints
{
    public class ExteriorWindow
        : BaseExterior<ExteriorWindow>
    {
        public bool Deny { get; private set; }

        private ExteriorWindow(bool deny)
            : base(!deny, Section.Types.Window)
        {
            Deny = deny;
        }

        internal override T Union<T>(T other)
        {
            return Union(other as ExteriorWindow) as T;
        }

        private ExteriorWindow Union(ExteriorWindow other)
        {
            Contract.Requires(other != null);

            return new ExteriorWindow(Deny || other.Deny);
        }

        public override bool IsSatisfied(FloorplanRegion region)
        {
            var window = region.Shape.Any(a => a.Sections.Any(s => s.Type == Section.Types.Window));
            return window ^ Deny;
        }

        internal class Container
            : BaseContainer
        {
            public bool Deny { get; [UsedImplicitly]set; }

            public override BaseSpaceConstraintSpec Unwrap(Func<double> random, INamedDataCollection metadata)
            {
                return new ExteriorWindow(Deny);
            }
        }
    }
}
