
namespace Base_CityGeneration.Elements.Block.Parcelling.Rules
{
    public class AreaRule
        :ITerminationRule
    {
        private readonly float _hardMinArea;
        private readonly float _maxArea;
        private readonly float _terminationChance;

        public AreaRule(float hardMinArea, float maxArea, float terminationChance)
        {
            _hardMinArea = hardMinArea;
            _maxArea = maxArea;
            _terminationChance = terminationChance;
        }

        public float TerminationChance(Parcel parcel)
        {
            return parcel.Area() < _maxArea ? _terminationChance : 0;
        }

        public bool Discard(Parcel parcel)
        {
            return parcel.Area() < _hardMinArea;
        }
    }
}
