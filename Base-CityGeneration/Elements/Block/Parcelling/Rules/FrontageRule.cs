
namespace Base_CityGeneration.Elements.Block.Parcelling.Rules
{
    public class FrontageRule
        :ITerminationRule
    {
        private readonly float _hardMinFrontage;
        private readonly float _maxFrontage;
        private readonly float _terminationChance;

        public FrontageRule(float hardMinFrontage, float maxFrontage, float terminationChance)
        {
            _hardMinFrontage = hardMinFrontage;
            _maxFrontage = maxFrontage;
            _terminationChance = terminationChance;
        }

        public float TerminationChance(Parcel parcel)
        {
            return parcel.MaxFrontage() < _maxFrontage ? _terminationChance : 0;
        }

        public bool Discard(Parcel parcel)
        {
            return parcel.MinFrontage() < _hardMinFrontage;
        }
    }
}
