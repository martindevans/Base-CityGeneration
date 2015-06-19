
using System.ComponentModel;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Selection.Spec
{
    /// <summary>
    /// References another floor in a spec
    /// </summary>
    public class FloorRef
    {
        private FloorRef(string @ref, ReferencePosition position, NormalValueSpec random)
        {
            Ref = @ref;
            Position = position;
            Random = random;
        }

        public string Ref { get; private set; }

        public ReferencePosition Position { get; private set; }

        public NormalValueSpec Random { get; private set; }

        internal class Container
        {
            public string Ref { get; set; }
            public ReferencePosition? Position { get; set; }

            public NormalValueSpec.Container Random { get; set; }

            public FloorRef Unwrap()
            {
                if (Position.HasValue && Position.Value == ReferencePosition.Random && Random == null)
                    throw new InvalidEnumArgumentException("Position cannot be random if Random is null");

                return new FloorRef(Ref, Position ?? ReferencePosition.Middle, Random == null ? null : Random.Unwrap());
            }
        }
    }

    public enum ReferencePosition
    {
        Top,
        Middle,
        Bottom,
        Random
    }
}
