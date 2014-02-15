using Base_CityGeneration.Elements.Block.Parcelling;

namespace Base_CityGeneration.Elements.Block
{
    public interface IParcelElement
    {
        /// <summary>
        /// The parcel which this element occupies
        /// </summary>
        Parcel Parcel { get; set; }
    }
}
