using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Myre.Extensions;

namespace Base_CityGeneration.Elements.Block.Parcelling
{
    public interface IParceller
    {
        /// <summary>
        /// Given the footprint of the entire block, generate parcels for buildings in the block
        /// </summary>
        /// <param name="root"></param>
        /// <returns></returns>
        IEnumerable<Parcel> GenerateParcels(Parcel root);

        /// <summary>
        /// Add a chance to terminate split recursion to this parceller
        /// </summary>
        void AddTerminationRule(ITerminationRule rule);
    }

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

    public struct Parcel
    {
        public readonly Edge[] Edges;

        public Parcel(Edge[] edges)
        {
            Edges = edges;
        }

        /// <summary>
        /// Construct a new parcel from a set of points, every point is assumed to have road access
        /// </summary>
        /// <param name="footprint"></param>
        /// <param name="edgeResources"></param>
        public Parcel(Vector2[] footprint, string[] edgeResources)
        {
            Edges = new Edge[footprint.Length];
            for (int i = 0; i < footprint.Length; i++)
                Edges[i] = new Edge { Start = footprint[i], End = footprint[(i + 1) % footprint.Length], Resources = edgeResources };
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
            return Points().Area();
        }

        /// <summary>
        /// The points which define the boundary of this parcel
        /// </summary>
        /// <returns></returns>
        public Vector2[] Points()
        {
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

            return front.Select(e => (e.End - e.Start).Length()).Max();
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
