using Base_CityGeneration.Elements.Roads.Hyperstreamline.Fields.Vectors;
using Microsoft.Xna.Framework;

namespace Base_CityGeneration.Elements.Roads.Hyperstreamline.Tracing
{
    struct Seed
    {
        public readonly Vector2 Point;
        public readonly IVector2Field Field;
        public readonly IVector2Field AlternativeField;

        public Seed(Vector2 point, IVector2Field field, IVector2Field alternativeField)
            : this()
        {
            Point = point;
            Field = field;
            AlternativeField = alternativeField;
        }
    }
}
