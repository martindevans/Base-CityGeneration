using System;
using System.Collections.Generic;
using System.Numerics;
using Base_CityGeneration.Utilities.Numbers;
using Myre.Collections;
using SwizzleMyVectors;
using SwizzleMyVectors.Geometry;
using MathHelper = Microsoft.Xna.Framework.MathHelper;

namespace Base_CityGeneration.Elements.Building.Design.Spec.Markers.Algorithms
{
    public class InvertCorner
        : BaseFootprintAlgorithm
    {
        private readonly IValueGenerator _angle;
        private readonly IValueGenerator _distance;
        private readonly IValueGenerator _minDistance;

        private readonly bool _inner;
        private readonly bool _outer;

        public InvertCorner(IValueGenerator angle, IValueGenerator distance, IValueGenerator minDistance, bool inner, bool outer)
        {
            _angle = angle;
            _distance = distance;
            _minDistance = minDistance;
            _inner = inner;
            _outer = outer;
        }

        public override IReadOnlyList<Vector2> Apply(Func<double> random, INamedDataCollection metadata, IReadOnlyList<Vector2> footprint, IReadOnlyList<Vector2> basis, IReadOnlyList<Vector2> lot)
        {
            var result = new List<Vector2>();

            for (var i = 0; i < footprint.Count; i++)
            {
                //Get points before, on and after this corner
                var a = footprint[(i + footprint.Count - 1) % footprint.Count];
                var b = footprint[i];
                var c = footprint[(i + 1) % footprint.Count];

                //Measure the angle
                var ab = b - a;
                var bc = c - b;
                var angle = Math.Acos(Vector2.Dot(ab, bc));

                //Determine corner type
                bool isInner = DetermineCornerType(ab, bc);

                //Invert this corner is this is the right type of corner
                if (angle > MathHelper.ToRadians(_angle.SelectFloatValue(random, metadata)) || ((isInner && !_inner) || (!isInner && !_outer)))
                {
                    //Angle is not acute enough, copy across this point to the result
                    result.Add(b);
                    continue;
                }

                
                //Select a distance for this bevel
                var distance = _distance.SelectFloatValue(random, metadata);

                //We need to bevel this angle
                var abLength = ab.Length();
                var bcLength = bc.Length();

                //Check that incut is not larger than the edge
                distance = Math.Min(distance, Math.Min(abLength * 0.5f, bcLength * 0.5f));

                //Terminate if distance is too small
                var min = _minDistance.SelectFloatValue(random, metadata);
                if (distance < min)
                {
                    result.Add(b);
                    continue;
                }

                //Point between A and B
                var b1 = a + (ab / abLength) * (abLength - distance);

                //Point between B and C
                var b2 = b + (bc / bcLength) * distance;

                //Peak of incut
                var intersect = new Ray2(b1, bc).Intersects(new Ray2(b2, ab));
                if (!intersect.HasValue)
                {
                    //Can't find a peak for this incut, just skip over this and leave this corner unchanged
                    result.Add(b);
                }
                else
                {
                    result.Add(b1);
                    result.Add(intersect.Value.Position);
                    result.Add(b2);
                }
            }

            return result;
        }

        private static bool DetermineCornerType(Vector2 ab, Vector2 bc)
        {
            return ab.Cross(bc) > 0;
        }

        public class Container
            : BaseContainer
        {
            /// <summary>
            /// Any corner with an internal angle less than this will be inverted
            /// </summary>
            public object Angle { get; set; }

            /// <summary>
            /// The distance back from the peak of the angle to invert
            /// </summary>
            public object Distance { get; set; }

            /// <summary>
            /// If the distance is reduced (due to the wall being too small) nothing will happen if the new distance is less than this distance
            /// </summary>
            public object MinDistance { get; set; }

            /// <summary>
            /// Whether or not inner corners (i.e. corners with the smallest angle on the inside of the building) should be inverted
            /// </summary>
            public bool InvertInner { get; set; }

            /// <summary>
            /// Whether or not outer corners (i.e. corners with the smallest angle on the outside of the building) should be inverted
            /// </summary>
            public bool InvertOuter { get; set; }

            internal override BaseFootprintAlgorithm Unwrap()
            {
                return new InvertCorner(
                    BaseValueGeneratorContainer.FromObject(Angle),
                    BaseValueGeneratorContainer.FromObject(Distance),
                    BaseValueGeneratorContainer.FromObject(MinDistance ?? 0),
                    InvertInner,
                    InvertOuter
                );
            }
        }
    }
}
