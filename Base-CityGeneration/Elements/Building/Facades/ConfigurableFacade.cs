using System;
using System.Collections.Generic;
using EpimetheusPlugins.Procedural;
using EpimetheusPlugins.Scripts;
using Myre.Collections;

namespace Base_CityGeneration.Elements.Building.Facades
{
    [Script("ACA20DC2-E38B-4F53-97A8-228D1E8F5009", "Externally Configurable Facade")]
    public class ConfigurableFacade
        : BaseFacade, IConfigurableFacade
    {
        public bool IsSubdivided { get; private set; }

        private INamedDataCollection _parameters;

        public override void Subdivide(Prism bounds, ISubdivisionGeometry geometry, INamedDataCollection hierarchicalParameters)
        {
            base.Subdivide(bounds, geometry, _parameters ?? hierarchicalParameters);

            IsSubdivided = true;
        }

        private readonly List<Stamp> _stamps = new List<Stamp>();
        public IEnumerable<Stamp> Stamps { get { return _stamps; } }

        public void AddStamp(Stamp stamp)
        {
            if (IsSubdivided)
                throw new InvalidOperationException("Cannot add stamp after facade has been subdivided");

            _stamps.Add(stamp);
        }

        protected override IEnumerable<Stamp> EmbossingStamps(INamedDataCollection hierarchicalParameters, float width, float height)
        {
            return Stamps;
        }

        public void SetHierarchicalParameters(INamedDataCollection parameters)
        {
            _parameters = parameters;
        }
    }
}
