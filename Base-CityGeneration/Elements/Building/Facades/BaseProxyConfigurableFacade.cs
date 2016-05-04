using EpimetheusPlugins.Procedural;
using System.Collections.Generic;
using Base_CityGeneration.Geometry.Walls;

namespace Base_CityGeneration.Elements.Building.Facades
{
    /// <summary>
    /// facade which passes all stamps to another configurable facade
    /// </summary>
    public abstract class BaseProxyConfigurableFacade
        : ProceduralScript, IConfigurableFacade
    {
        public IConfigurableFacade Facade { get; set; }

        public Section Section
        {
            get { return Facade.Section; }
            set { Facade.Section = value; }
        }

        public IEnumerable<BaseFacade.Stamp> Stamps
        {
            get { return Facade.Stamps; }
        }

        public void AddStamp(BaseFacade.Stamp stamp)
        {
            Facade.AddStamp(stamp);
        }


        public ISubdivisionContext GetDependencyContext()
        {
            return Facade.GetDependencyContext();
        }
    }
}
