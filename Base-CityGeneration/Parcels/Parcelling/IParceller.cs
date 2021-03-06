﻿using System.Numerics;
using Base_CityGeneration.Datastructures;
using Myre.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using SwizzleMyVectors;

namespace Base_CityGeneration.Parcels.Parcelling
{
    [ContractClass(typeof(IParcellerContracts))]
    public interface IParceller
    {
        /// <summary>
        /// Given the footprint of the entire block, generate parcels for buildings in the block
        /// </summary>
        /// <param name="root"></param>
        /// <param name="random"></param>
        /// <param name="metadata"></param>
        /// <returns></returns>
        IEnumerable<Parcel> GenerateParcels(Parcel root, Func<double> random, INamedDataCollection metadata);
    }

    [ContractClassFor(typeof(IParceller))]
    internal abstract class IParcellerContracts
        : IParceller
    {
        public IEnumerable<Parcel> GenerateParcels(Parcel root, Func<double> random, INamedDataCollection metadata)
        {
            Contract.Requires(root != null);
            Contract.Requires(random != null);
            Contract.Requires(metadata != null);
            Contract.Ensures(Contract.Result<IEnumerable<Parcel>>() != null);

            return default(IEnumerable<Parcel>);
        }
    }

    [ContractClass(typeof(ITerminatedRuleContracts))]
    public interface ITerminationRule
    {
        /// <summary>
        /// Returns the chance of termination by this rule. If the chance is zero (on any rule) then termination will not happen (except if discard comes into play)
        /// </summary>
        /// <param name="parcel"></param>
        /// <returns></returns>
        float? TerminationChance(Parcel parcel);

        /// <summary>
        /// If this parcel is invalid. If *any* rules requires discard then the parcel will be discarded and recursion terminated (possibly violating a termination chance of zero)
        /// </summary>
        /// <param name="parcel"></param>
        /// <param name="random"></param>
        /// <returns></returns>
        bool Discard(Parcel parcel, Func<double> random);
    }

    [ContractClassFor(typeof(ITerminationRule))]
    internal abstract class ITerminatedRuleContracts
        : ITerminationRule
    {
        public float? TerminationChance(Parcel parcel)
        {
            Contract.Requires(parcel != null);

            return default(float?);
        }

        public bool Discard(Parcel parcel, Func<double> random)
        {
            Contract.Requires(parcel != null);
            Contract.Requires(random != null);

            return default(bool);
        }
    }

    public class Parcel
    {
        public readonly Rectangle Bounds;
        public readonly Edge[] Edges;

        public Parcel(Edge[] edges)
        {
            Contract.Requires(edges != null);

            Edges = edges;

            Bounds = Rectangle.FromPoints(Points());
        }

        [ContractInvariantMethod]
        private void ObjectInvariants()
        {
            Contract.Invariant(Edges != null);
        }

        /// <summary>
        /// Construct a new parcel from a set of points, every point is assumed to have road access
        /// </summary>
        /// <param name="footprint"></param>
        /// <param name="edgeResources"></param>
        public Parcel(IEnumerable<Vector2> footprint, string[] edgeResources)
        {
            Contract.Requires(footprint != null);

            var footprintArr = footprint.ToArray();

            if (footprintArr.Area() < 0)
                Array.Reverse(footprintArr);

            Edges = new Edge[footprintArr.Length];
            for (var i = 0; i < footprintArr.Length; i++)
                Edges[i] = new Edge { Start = footprintArr[i], End = footprintArr[(i + 1) % footprintArr.Length], Resources = edgeResources };
        }

        public struct Edge
        {
            public Vector2 Start;
            public Vector2 End;
            public string[] Resources;
        }

        /// <summary>
        /// The total area of this parcel
        /// </summary>
        /// <returns></returns>
        public float Area()
        {
            return Math.Abs(Points().Area());
        }

        public float AspectRatio()
        {
            var oabb = OABR.Fit(Points());
            var extents = oabb.Max - oabb.Min;
            var ratio = Math.Max(extents.X, extents.Y) / Math.Min(extents.X, extents.Y);

            return ratio;
        }

        /// <summary>
        /// The points which define the boundary of this parcel
        /// </summary>
        /// <returns></returns>
        public Vector2[] Points()
        {
            Contract.Ensures(Contract.Result<Vector2[]>() != null);

            return Edges.Select(e => e.Start).ToArray();
        }

        /// <summary>
        /// The longest edge of this parcel which has road access
        /// </summary>
        /// <returns></returns>
        public float? MaxAccessFrontage(string resource)
        {
            var front = Edges.Where(e => e.Resources.Contains(resource));
            if (!front.Any())
                return null;

            var l = front.Select(e => (e.End - e.Start).Length()).Max();
            return l;
        }

        /// <summary>
        /// The shortest edge of this parcel which has road access
        /// </summary>
        /// <returns></returns>
        public float? MinAccessFrontage(string resource)
        {
            var front = Edges.Where(e => e.Resources.Contains(resource));
            if (!front.Any())
                return null;

            return front.Select(e => (e.End - e.Start).Length()).Min();
        }

        /// <summary>
        /// Indicates if this parcel has road access
        /// </summary>
        /// <returns></returns>
        public bool HasAccess(string resource)
        {
            return Edges.Any(a => a.Resources.Contains(resource));
        }
    }
}
