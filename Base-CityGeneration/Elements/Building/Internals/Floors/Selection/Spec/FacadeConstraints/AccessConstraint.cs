
namespace Base_CityGeneration.Elements.Building.Internals.Floors.Selection.Spec.FacadeConstraints
{
    public class OpenSpaceConstraint
        : BaseFacadeConstraint
    {
        public string Type { get; private set; }

        private OpenSpaceConstraint(string type)
        {
            Type = type;
        }

        internal class Container
            : BaseContainer
        {
            public string Type { get; set; }

            public override BaseFacadeConstraint Unwrap()
            {
                return new OpenSpaceConstraint(Type);
            }
        }
    }
}
