using System.Linq;
using Base_CityGeneration.Utilities.Numbers;
using Myre.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Numerics;
using SwizzleMyVectors;

namespace Base_CityGeneration.Elements.Building.Design.Spec.Markers.Algorithms
{
    public class Twist
        : BaseFootprintAlgorithm
    {
        private readonly IValueGenerator _angle;

        public Twist(IValueGenerator angle)
        {
            Contract.Requires(angle != null);

            _angle = angle;
        }

        public override IReadOnlyList<Vector2> Apply(Func<double> random, INamedDataCollection metadata, IReadOnlyList<Vector2> footprint, IReadOnlyList<Vector2> basis, IReadOnlyList<Vector2> lot)
        {
            var center = footprint.Aggregate((a, b) => a + b) / footprint.Count;
            var radians = Microsoft.Xna.Framework.MathHelper.ToRadians(_angle.SelectFloatValue(random, metadata));

            return footprint.Select(a => Vector3.Transform((a - center).X_Y(0), Quaternion.CreateFromAxisAngle(Vector3.UnitY, radians)).XZ() + center).ToArray();
        }

        internal class Container
            : BaseContainer
        {
            public object Angle { get; set; }

            public override BaseFootprintAlgorithm Unwrap()
            {
                return new Twist(
                    IValueGeneratorContainer.FromObject(Angle ?? 0)
                );
            }
        }
    }
}
