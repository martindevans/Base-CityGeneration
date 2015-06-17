using Base_CityGeneration.Datastructures.HalfEdge;
using Microsoft.Xna.Framework;
using System.Linq;

namespace Base_CityGeneration.Elements.Roads
{
    public class FaceBlockBuilder : IFaceBuilder
    {
        public Face<IVertexBuilder, IHalfEdgeBuilder, IFaceBuilder> Face { get; private set; }

        private Vector2[] _footprint = null;
        public Vector2[] Shape
        {
            get
            {
                if (_footprint == null)
                    _footprint = CalculateShape();
                return _footprint;
            }
        }

        public FaceBlockBuilder(Face<IVertexBuilder, IHalfEdgeBuilder, IFaceBuilder> face)
        {
            Face = face;
        }

        private Vector2[] CalculateShape()
        {
            return (
                from halfEdge in Face.Edges
                let builder = halfEdge.IsPrimaryEdge ? halfEdge.Tag : halfEdge.Pair.Tag
                select halfEdge.IsPrimaryEdge ? builder.RightStart : builder.LeftEnd
            ).ToArray();
        }
    }
}
