using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Numerics;
using Base_CityGeneration.Elements.Building.Design;
using Base_CityGeneration.Elements.Building.Internals.VerticalFeatures;
using EpimetheusPlugins.Entities;
using EpimetheusPlugins.Procedural;
using Myre.Collections;
using SwizzleMyVectors.Geometry;

namespace Base_CityGeneration.Elements.Building.Internals.Floors
{
    [ContractClass(typeof(IFloorContracts))]
    public interface IFloor
        : ISubdivisionContext
    {
        /// <summary>
        /// Index of this floor in parent building (ground floor is zero, basements are negative)
        /// </summary>
        int FloorIndex { get; set; }

        /// <summary>
        /// Altitude of the *base* of this floor
        /// </summary>
        float FloorAltitude { get; set; }

        /// <summary>
        /// Height of this floor
        /// </summary>
        float FloorHeight { get; set; }

        /// <summary>
        /// Vertical features which cross this floor
        /// </summary>
        IReadOnlyCollection<IVerticalFeature> Overlaps { set; }

        /// <summary>
        /// Choose the footprint for a given vertical feature
        /// </summary>
        /// <param name="feature">The vertical feature we are placing (which *starts* at this floor)</param>
        /// <param name="space">The available space to place the vertical feature within (may be restricted by higher floors being a different shape)</param>
        /// <param name="floors">The floors which this vertical feature crosses</param>
        /// <returns>The footprint of this feature</returns>
        IEnumerable<Vector2> PlaceVerticalFeature(VerticalSelection feature, IReadOnlyList<Vector2> space, IReadOnlyList<IFloor> floors);
    }

    [ContractClassFor(typeof(IFloor))]
    internal abstract class IFloorContracts
        : IFloor
    {
        int IFloor.FloorIndex
        {
            get { return default(int); }
            set { }
        }

        float IFloor.FloorAltitude
        {
            get { return default(float); }
            set { }
        }

        float IFloor.FloorHeight
        {
            get { return default(float); }
            set { }
        }

        IReadOnlyCollection<IVerticalFeature> IFloor.Overlaps
        {
            set { }
        }

        IEnumerable<Vector2> IFloor.PlaceVerticalFeature(VerticalSelection feature, IReadOnlyList<Vector2> space, IReadOnlyList<IFloor> floors)
        {
            Contract.Requires(feature != null);
            Contract.Requires(space != null);
            Contract.Requires(floors != null);

            Contract.Ensures(Contract.Result<IEnumerable<Vector2>>() != null);
            Contract.Ensures(Contract.Result<IEnumerable<Vector2>>().Count() >= 3);

            return default(IEnumerable<Vector2>);
        }

        void ISubdivisionContext.AddPrerequisite(ISubdivisionContext prerequisite, bool recursive)
        {
        }

        BoundingBox ISubdivisionContext.BoundingBox
        {
            get { return default(BoundingBox); }
        }

        Prism ISubdivisionContext.Bounds
        {
            get { return default(Prism); }
        }

        IEnumerable<ProceduralScript> ISubdivisionContext.Children
        {
            get { return default(IEnumerable<ProceduralScript>); }
        }

        Guid ISubdivisionContext.Guid
        {
            get { return default(Guid); }
        }

        INamedDataCollection ISubdivisionContext.HierarchicalParameters
        {
            get { return default(INamedDataCollection); }
            set { }
        }

        Matrix4x4 ISubdivisionContext.InverseWorldTransformation
        {
            get { return default(Matrix4x4); }
        }

        INamedDataCollection ISubdivisionContext.Metadata
        {
            get { return default(INamedDataCollection); }
        }

        ProceduralScript ISubdivisionContext.Parent
        {
            get { return default(ProceduralScript); }
        }

        SubdivisionStates ISubdivisionContext.State
        {
            get { return default(SubdivisionStates); }
        }

        Matrix4x4 ISubdivisionContext.WorldTransformation
        {
            get { return default(Matrix4x4); }
        }

        IEntity EpimetheusPlugins.Services.EntityCreation.IEntityCreator.Create(EpimetheusPlugins.Scripts.ScriptReference script, NamedBoxCollection data)
        {
            return default(IEntity);
        }
    }
}
