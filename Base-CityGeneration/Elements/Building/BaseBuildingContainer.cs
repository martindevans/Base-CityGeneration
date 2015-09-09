using EpimetheusPlugins.Procedural;
using Myre.Collections;
using System;

namespace Base_CityGeneration.Elements.Building
{
    /// <summary>
    /// Represents an abstract "container" of a building from which height can be calculated. This is used by neighbouring buildings when creating external facades
    /// </summary>
    public abstract class BaseBuildingContainer
        : ProceduralScript, IBuildingContainer
    {
        private readonly float _minHeight;
        private readonly float _maxHeight;

        private float? _height;
        public float Height
        {
            get
            {
                if (!_height.HasValue)
                    throw new InvalidOperationException("Cannot get height of building container before container is subdivided");
                return _height.Value;
            }
        }

        protected BaseBuildingContainer(float minHeight, float maxHeight)
        {
            _minHeight = minHeight;
            _maxHeight = maxHeight;
        }

        public override bool Accept(Prism bounds, INamedDataProvider parameters)
        {
            return bounds.Height <= _maxHeight
                && bounds.Height >= _minHeight;
        }

        public override void Subdivide(Prism bounds, ISubdivisionGeometry geometry, INamedDataCollection hierarchicalParameters)
        {
            _height = CalculateHeight();
        }

        protected abstract float CalculateHeight();
    }

    /// <summary>
    /// Represents an abstract "container" of a building from which height can be calculated. This is used by neighbouring buildings when creating external facades
    /// </summary>
    public interface IBuildingContainer
        : ISubdivisionContext
    {
        float Height { get; }
    }
}
