using System;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;
using Base_CityGeneration.Elements.Roads.Hyperstreamline.Fields.Tensors;
using Base_CityGeneration.Elements.Roads.Hyperstreamline.Fields.Vectors;
using System.Numerics;

using MathHelper = Microsoft.Xna.Framework.MathHelper;

namespace Base_CityGeneration.Elements.Roads.Hyperstreamline.Fields.Eigens
{
    class ResampleAndRescale
        : IEigenField
    {
        private readonly IVector2Field _major;
        public IVector2Field MajorEigenVectors { get { return _major; } }

        private readonly IVector2Field _minor;
        public IVector2Field MinorEigenVectors { get { return _minor; } }

        private readonly Vector2[,] _majorEigenVectors;
        private readonly float _xLength;
        private readonly float _yLength;

        private readonly Vector2 _size;
        private readonly Vector2 _min;

        private readonly bool _zeroSize;

        private ResampleAndRescale(Vector2[,] major, Vector2 min, Vector2 max)
        {
            _min = min;
            _size = max - min;

            _zeroSize = (Math.Abs(_size.X) < float.Epsilon || Math.Abs(_size.Y) < float.Epsilon);

            if (_zeroSize)
                return;

            _majorEigenVectors = major;

            _xLength = major.GetLength(0) - 1;
            _yLength = major.GetLength(1) - 1;

            _minor = new EigenAccessor(true, this);
            _major = new EigenAccessor(false, this);
        }

        private void Sample(bool major, ref Vector2 position, out Vector2 result)
        {
            //If we've already established that this array is zero size, return nothing
            if (_zeroSize)
            {
                result = new Vector2(0, 0);
                return;
            }

            //Work out array position from vector field position
            var p = ((position - _min) / _size);
            var ij = p * new Vector2(_xLength, _yLength);
            var i = (int)MathHelper.Clamp(ij.X, 0, _xLength);
            var j = (int)MathHelper.Clamp(ij.Y, 0, _yLength);

            //Result from array (no interpolation)
            result = _majorEigenVectors[i, j];

            //Flip it from major to minor as necessary
            if (!major)
                result = new Vector2(-result.Y, result.X);
        }

        public static IEigenField Create(ITensorField baseField, Vector2 min, Vector2 max, uint resolution)
        {
            var major = new Vector2[resolution + 1, resolution + 1];

            Parallel.For(0, resolution + 1, i =>
                Parallel.For(0, resolution + 1, j => {
                    var p = new Vector2(i / (float)resolution, j / (float)resolution) + (min / new Vector2(resolution, resolution));

                    Tensor t;
                    baseField.Sample(ref p, out t);

                    Vector2 majEigen, minEigen;
                    t.EigenVectors(out majEigen, out minEigen);

                    major[i, j] = majEigen;
                })
            );

            return new ResampleAndRescale(major, min, max);
        }

        private class EigenAccessor
            : IVector2Field
        {
            private readonly bool _major;
            private readonly ResampleAndRescale _field;

            public EigenAccessor(bool major, ResampleAndRescale field)
            {
                Contract.Requires(field != null);

                _major = major;
                _field = field;
            }

            [ContractInvariantMethod]
            private void ObjectInvariants()
            {
                Contract.Invariant(_field != null);
            }

            public Vector2 Sample(Vector2 position)
            {
                Vector2 result;
                _field.Sample(_major, ref position, out result);
                return result;
            }
        }
    }
}
