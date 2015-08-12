using System;

namespace Base_CityGeneration.Parcels.Parcelling.Rules
{
    /// <summary>
    /// Provides a chance to subdivide once blocks are below a max area
    /// </summary>
    public class AreaRule
        : ITerminationRule

    {
        private readonly float _hardMinArea;
        private readonly float _maxArea;
        private readonly float _terminationChance;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hardMinArea">The area which blocks may definitely not be smaller than</param>
        /// <param name="maxArea">The area below which blocks gain a chance to terminate subdivision</param>
        /// <param name="terminationChance">The chance to terminate subdivision</param>
        public AreaRule(float hardMinArea, float maxArea, float terminationChance)
        {
            _hardMinArea = hardMinArea;
            _maxArea = maxArea;
            _terminationChance = terminationChance;
        }

        public float? TerminationChance(Parcel parcel)
        {
            return parcel.Area() < _maxArea ? _terminationChance : 0;
        }

        public bool Discard(Parcel parcel, Func<double> random)
        {
            var a = parcel.Area();
            return a < _hardMinArea;
        }
    }
}
