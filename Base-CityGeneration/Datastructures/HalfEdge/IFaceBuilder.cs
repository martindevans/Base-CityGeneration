using Microsoft.Xna.Framework;

namespace Base_CityGeneration.Datastructures.HalfEdge
{
    public interface IFaceBuilder
    {
        Face Face { get; }

        Vector2[] Shape { get; }
    }
}
