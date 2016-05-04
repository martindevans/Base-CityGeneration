using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Numerics;
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
