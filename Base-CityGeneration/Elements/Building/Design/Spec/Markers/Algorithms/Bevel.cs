using System;
using System.Collections.Generic;
using System.Numerics;
using Base_CityGeneration.Utilities.Numbers;
using Myre.Collections;
using MathHelper = Microsoft.Xna.Framework.MathHelper;

namespace Base_CityGeneration.Elements.Building.Design.Spec.Markers.Algorithms
{
    public class Bevel
        : BaseFootprintAlgorithm
    {
        private readonly IValueGenerator _angle;
        private readonly IValueGenerator _distance;

        public Bevel(IValueGenerator angle, IValueGenerator distance)
        {
            _angle = angle;
            _distance = distance;
        }

        public override IReadOnlyList<Vector2> Apply(Func<double> random, INamedDataCollection metadata, IReadOnlyList<Vector2> footprint, IReadOnlyList<Vector2> basis, IReadOnlyList<Vector2> lot)
        {
            List<Vector2> result = new List<Vector2>();

            for (int i = 0; i < footprint.Count; i++)
            {
                //Get points before, on and after this corner
                var a = footprint[(i + footprint.Count - 1) % footprint.Count];
                var b = footprint[i];
                var c = footprint[(i + 1) % footprint.Count];

                //Measure the angle
                var ab = b - a;
                var bc = c - b;
                var angle = Math.Acos(Vector2.Dot(ab, bc));

                if (angle > MathHelper.ToRadians(_angle.SelectFloatValue(random, metadata)))
                {
                    //Angle is not acute enough, copy across this point to the result
                    result.Add(b);
                }
                else
                {
                    //Select a distance for this bevel
                    var distance = _distance.SelectFloatValue(random, metadata);

                    //We need to bevel this angle
                    var abLength = ab.Length();
                    var bcLength = bc.Length();

                    //Check that bevel is not larger than half the edge
                    distance = Math.Min(distance, Math.Min(abLength * 0.5f, bcLength * 0.5f));

                    //Point between A and B
                    var b1 = a + (ab / abLength) * (abLength - distance);

                    //Point between B and C
                    var b2 = b + (bc / bcLength) * distance;

                    result.Add(b1);
                    result.Add(b2);
                }
            }

            return result;
        }

        public class Container
            : BaseContainer
        {
            /// <summary>
            /// Any corner with an internal angle less than this will be bevelled
            /// </summary>
            public object Angle { get; set; }

            /// <summary>
            /// The distance back from the peak of the angle to bevel
            /// </summary>
            public object Distance { get; set; }

            internal override BaseFootprintAlgorithm Unwrap()
            {
                return new Bevel(
                    BaseValueGeneratorContainer.FromObject(Angle),
                    BaseValueGeneratorContainer.FromObject(Distance)
                );
            }
        }
    }
}
