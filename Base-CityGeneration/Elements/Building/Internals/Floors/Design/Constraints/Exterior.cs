namespace Base_CityGeneration.Elements.Building.Internals.Floors.Design.Constraints
{
    /// <summary>
    /// Indicates that this space must border an exterior wall
    /// </summary>
    public class Exterior
        : BaseSpaceConstraintSpec
    {
        /// <summary>
        /// If not null - indicates that this space *must* or *must not* have an exterior door
        /// </summary>
        public bool? Door { get; private set; }

        /// <summary>
        /// If not null - indicates that this space *must* or *must not* have an exterior window
        /// </summary>
        public bool? Window { get; private set; }

        private Exterior(bool? door = null, bool? window = null)
        {
            Door = door;
            Window = window;
        }

        internal class Container
            : BaseContainer
        {
            public bool? Door { get; set; }
            public bool? Window { get; set; }

            public override BaseSpaceConstraintSpec Unwrap()
            {
                return new Exterior(Door, Window);
            }
        }
    }
}
