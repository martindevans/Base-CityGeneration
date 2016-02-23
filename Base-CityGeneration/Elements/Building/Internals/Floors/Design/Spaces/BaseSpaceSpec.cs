using JetBrains.Annotations;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Design.Spaces
{
    public abstract class BaseSpaceSpec
        : ISpec
    {
        /// <summary>
        /// ID of this floor, used for debugging and error message purposes
        /// </summary>
        public string Id { get; private set; }

        protected BaseSpaceSpec(string id)
        {
            Id = id;
        }

        internal abstract class BaseContainer
            : IUnwrappable<BaseSpaceSpec>
        {
            // ReSharper disable MemberCanBeProtected.Global
            public string Id { get; [UsedImplicitly]set; }
            // ReSharper restore MemberCanBeProtected.Global

            public abstract BaseSpaceSpec Unwrap();
        }
    }
}
