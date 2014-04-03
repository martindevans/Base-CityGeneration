
namespace Base_CityGeneration.Parcelling
{
    public interface IParcelElement<T> where T : class, IParcelElement<T>
    {
        /// <summary>
        /// The parcel which this element occupies
        /// </summary>
        Parcel<T> Parcel { get; set; }
    }
}
