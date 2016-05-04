using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Base_CityGeneration.Elements.Building.Design;

namespace Base_CityGeneration.Elements.Building.Internals.VerticalFeatures
{
    /// <summary>
    /// A container of vertical elements. vertical features can be added and then queried later
    /// </summary>
    [ContractClass(typeof(IVerticalFeatureContainerContracts))]
    public interface IVerticalFeatureContainer
    {
        /// <summary>
        /// Add a vertical feature to this container. Should throw if any of the overlapped floors are already subdivided
        /// </summary>
        /// <param name="selection"></param>
        /// <param name="feature"></param>
        void Add(VerticalSelection selection, IVerticalFeature feature);

        /// <summary>
        /// Find all overlapping elements for the given floor.
        /// </summary>
        /// <param name="floor"></param>
        /// <param name="checkSubdivided">If set then this method will throw if the given floor is not yet subdivided</param>
        /// <returns></returns>
        IEnumerable<KeyValuePair<VerticalSelection, IVerticalFeature>> Overlapping(int floor, bool checkSubdivided = true);
    }

    [ContractClassFor(typeof(IVerticalFeatureContainer))]
    public abstract class IVerticalFeatureContainerContracts
        : IVerticalFeatureContainer
    {
        public void Add(VerticalSelection selection, IVerticalFeature feature)
        {
            Contract.Requires(selection != null);
            Contract.Requires(feature != null);
        }

        public IEnumerable<KeyValuePair<VerticalSelection, IVerticalFeature>> Overlapping(int floor, bool checkSubdivided = true)
        {
            Contract.Ensures(Contract.Result<IEnumerable<KeyValuePair<VerticalSelection, IVerticalFeature>>>() != null);

            return null;
        }
    }
}
