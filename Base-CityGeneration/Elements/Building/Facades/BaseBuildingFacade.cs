
namespace Base_CityGeneration.Elements.Building.Facades
{
    public abstract class BaseBuildingFacade
        : BaseProxyConfigurableFacade, IBuildingFacade
    {
        public int BottomFloorIndex { get; set; }

        public int TopFloorIndex { get; set; }
    }
}
