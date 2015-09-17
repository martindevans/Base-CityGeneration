
namespace Base_CityGeneration.Elements.Building.Design.Spec.FacadeConstraints
{
    public class AccessConstraint
        : BaseFacadeConstraint
    {
        public string Type { get; private set; }

        private AccessConstraint(string type)
        {
            Type = type;
        }

        public override bool Check(FloorSelection floor, BuildingSideInfo[] neighbours, System.Numerics.Vector2 edgeStart, System.Numerics.Vector2 edgeEnd)
        {
            throw new System.NotImplementedException();
        }

        internal class Container
            : BaseContainer
        {
            public string Type { get; set; }

            public override BaseFacadeConstraint Unwrap()
            {
                return new AccessConstraint(Type);
            }
        }
    }
}
