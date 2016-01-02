using System.Diagnostics.Contracts;
using System.Numerics;

namespace Base_CityGeneration.Elements.Roads.Hyperstreamline.Fields.Vectors
{
    public interface IVector2Field
    {
        Vector2 Sample(Vector2 position);
    }

    internal interface IVector2FieldContainer
    {
        IVector2Field Unwrap();
    }

    internal static class InternalIVector2FieldExtensions
    {
        public static Vector2 TraceVectorField(this IVector2Field field, Vector2 position, Vector2 previous, float maxSegmentLength)
        {
            Vector2 first = previous;

            //Extended naieve tracing (accumulate naieve traces, stop once we hit sample OR length limit)
            float lengthSum = 0;
            Vector2 result = Vector2.Zero;
            for (int i = 0; i < 10 && lengthSum < maxSegmentLength; i++)
            {
                var d = field.SingleTraceVectorField(position, previous);
                var l = d.Length();
                lengthSum += d.Length();
                result += d;
                previous = d;

                //escape if the curvature is too much
                if (Vector2.Dot(first, d / l) < 0.9961f)
                    break;
            }

            return result;
        }

        private static Vector2 SingleTraceVectorField(this IVector2Field field, Vector2 position, Vector2 previous)
        {
            //Naieve tracing
            //return field.CorrectedSample(position, previous);

            //RK4
            var k1 = field.CorrectedSample(position, previous);
            var k2 = field.CorrectedSample(position + k1 / 2f, previous);
            var k3 = field.CorrectedSample(position + k2 / 2f, previous);
            var k4 = field.CorrectedSample(position + k3, previous);
            return CorrectVectorDirection(k1 / 6f + k2 / 3f + k3 / 3f + k4 / 6f, previous);
        }

        private static Vector2 CorrectedSample(this IVector2Field field, Vector2 position, Vector2 baseDirection)
        {
            return CorrectVectorDirection(field.Sample(position), baseDirection);
        }

        private static Vector2 CorrectVectorDirection(Vector2 v, Vector2 previous)
        {
            if (previous == Vector2.Zero || Vector2.Dot(previous, v) >= 0)
                return v;
            return -v;
        }
    }

    public static class IVector2FieldExtensions
    {
        public static IVector2Field Inverse(this IVector2Field field)
        {
            Contract.Requires(field != null);

            return new Inverse(field);
        }
    }
}
