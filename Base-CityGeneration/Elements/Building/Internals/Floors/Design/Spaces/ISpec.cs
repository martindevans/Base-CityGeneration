using System;
using System.Collections.Generic;
using Base_CityGeneration.Elements.Building.Internals.Floors.Design.Spaces.Constraints;
using Base_CityGeneration.Elements.Building.Internals.Floors.Design.Spaces.Selector;
using Myre.Collections;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Design.Spaces
{
    public interface ISpec
    {
    }

    /// <summary>
    /// A spec for an actual space which occupies part of a floorplan
    /// </summary>
    public interface ISpaceSpec
    {
        /// <summary>
        /// ID of this space
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Indicates if this space may be used to connect to other spaces (i.e. people may walk through this space to get to the spaces)
        /// </summary>
        bool Walkthrough { get; }

        /// <summary>
        /// Indicates if entry elements (vertical elements or external doors) may be attached directly to this space (e.g. some kind of lobby)
        /// </summary>
        bool EntrySpace { get; }

        /// <summary>
        /// Constraints on the layout of this space
        /// </summary>
        IEnumerable<Weighted<BaseConstraint>> Constraints { get; }

        /// <summary>
        /// Connections to other spaces
        /// </summary>
        IEnumerable<Weighted<BaseSpecSelector>> Connections { get; }
    }

    /// <summary>
    /// A spec which produces more specs
    /// </summary>
    public interface IProviderSpec
    {
        /// <summary>
        /// Produce more specs
        /// </summary>
        /// <param name="random"></param>
        /// <param name="metadata"></param>
        /// <returns></returns>
        IEnumerable<ISpec> Expand(Func<double> random, INamedDataCollection metadata);
    }
}
