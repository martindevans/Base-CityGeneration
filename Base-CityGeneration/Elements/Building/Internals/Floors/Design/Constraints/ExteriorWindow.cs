using System;
using JetBrains.Annotations;

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
            throw new NotImplementedException();
        }

        internal class Container
            : BaseContainer
        {
            public bool Deny { get; [UsedImplicitly]set; }

            public override BaseSpaceConstraintSpec Unwrap()
            {
                return new ExteriorWindow(Deny);
            }
        }
    }
}
