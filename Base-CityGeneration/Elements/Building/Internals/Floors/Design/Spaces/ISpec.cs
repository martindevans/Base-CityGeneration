using System;
using System.Collections.Generic;
using Myre.Collections;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Design.Spaces
{
    public interface ISpec
    {
        string Id { get; }
    }

    /// <summary>
    /// A spec for an actual space which occupies part of a floorplan
    /// </summary>
    public interface ISpaceSpec
    {
        /// <summary>
        /// Indicates if this space may be used to connect to other spaces (i.e. people may walk through this space to get to the spaces)
        /// </summary>
        bool Walkthrough { get; }

        /// <summary>
        /// Indicates if entry elements (vertical elements or external doors) may be attached directly to this space (e.g. some kind of lobby)
        /// </summary>
        bool EntrySpace { get; }

        //todo: constraints
        //todo: connections
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
