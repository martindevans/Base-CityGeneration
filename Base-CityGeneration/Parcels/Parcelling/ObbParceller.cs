﻿using Base_CityGeneration.Utilities.Numbers;
using EpimetheusPlugins.Procedural.Utilities;
using System.Numerics;
using Myre.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Base_CityGeneration.Datastructures;
using SwizzleMyVectors;
using SwizzleMyVectors.Geometry;

namespace Base_CityGeneration.Parcels.Parcelling
{
    /// <summary>
    /// Parcels an area by recursively splitting the area along the middle of the OBB of the area
    /// </summary>
    public class ObbParceller
        :IParceller
    {
        private readonly IValueGenerator _nonOptionalObbChance;
        private readonly IValueGenerator _nonOptionalObbMaxRatio;
        private readonly IValueGenerator _splitPoint;

        private readonly List<ITerminationRule> _terminators = new List<ITerminationRule>();

        public ObbParceller(IValueGenerator splitPointGenerator = null, IValueGenerator nonOptimalOabbChance = null, IValueGenerator nonOptimalOabbMaxRatio = null)
        {
            Contract.Requires(splitPointGenerator == null || (splitPointGenerator.MinValue >= -1 && splitPointGenerator.MaxValue <= 1));
            Contract.Requires(nonOptimalOabbChance == null || (nonOptimalOabbChance.MinValue >= 0 && nonOptimalOabbChance.MaxValue <= 1));
            Contract.Requires(nonOptimalOabbMaxRatio == null || nonOptimalOabbMaxRatio.MinValue >= 1);

            _splitPoint = splitPointGenerator ?? new NormallyDistributedValue(-0.35f, 0.0f, 0.35f, 0.2f);
            _nonOptionalObbChance = nonOptimalOabbChance ?? new ConstantValue(0);
            _nonOptionalObbMaxRatio = nonOptimalOabbMaxRatio ?? new ConstantValue(0);
        }

        [ContractInvariantMethod]
        private void ObjectInvariants()
        {
            Contract.Invariant(_nonOptionalObbChance != null);
            Contract.Invariant(_nonOptionalObbMaxRatio != null);
            Contract.Invariant(_splitPoint != null);
            Contract.Invariant(_terminators != null);
        }

        public void AddTerminationRule(ITerminationRule rule)
        {
            _terminators.Add(rule);
        }

        public IEnumerable<Parcel> GenerateParcels(Parcel root, Func<double> random, INamedDataCollection metadata)
        {
            return RecursiveSplit(root, random, metadata);
        }

        private IEnumerable<Parcel> RecursiveSplit(Parcel parcel, Func<double> random, INamedDataCollection metadata)
        {
            Contract.Requires(parcel != null);
            Contract.Requires(random != null);
            Contract.Requires(metadata != null);

            //Accumulate chance of termination, checking for any rule which forbods it (i.e. probability zero)
            float accumulator = 0;
            var noChance = false;
            for (var i = 0; i < _terminators.Count && !noChance; i++)
            {
                var c = _terminators[i].TerminationChance(parcel);
                if (c.HasValue)
                {
                    accumulator += c.Value;
                    noChance |= c.Value <= 0;
                }
            }

            //If random chance beats average of all termination chances then stop here
            if (accumulator / _terminators.Count >= random())
                return new[] { parcel };

            var oabb = FitOabb(parcel, _nonOptionalObbChance.SelectFloatValue(random, metadata), _nonOptionalObbChance.SelectFloatValue(random, metadata), random);

            var splitLine = oabb.SplitDirection();
            var children = Split(parcel, oabb, splitLine, random, metadata).ToArray();

            //If any children are discarded try splitting the other way
            // ReSharper disable once AccessToModifiedClosure
            if (_terminators.Any(t => children.Any(c => t.Discard(c, random))))
            {
                splitLine = splitLine.Perpendicular();
                children = Split(parcel, oabb, splitLine, random, metadata).ToArray();
            }

            //Either return this parcel because we can't find any valid children, or continue recursive splitting
            if (_terminators.Any(t => children.Any(c => t.Discard(c, random))))
                return new[] { parcel };
            else
                return children.SelectMany(a => RecursiveSplit(a, random, metadata)).ToArray();
        }

        #region static helpers
        private IEnumerable<Parcel> Split(Parcel parcel, OABR oabb, Vector2 sliceDirection, Func<double> random, INamedDataCollection metadata)
        {
            Contract.Requires(parcel != null);
            Contract.Requires(random != null);
            Contract.Requires(metadata != null);

            var extent = (oabb.Max - oabb.Min) / 2;
            var point = oabb.Middle + Math.Max(extent.X, extent.Y) * sliceDirection.Perpendicular() * _splitPoint.SelectFloatValue(random, metadata);

            var slices = parcel.Points().SlicePolygon(new Ray2(point, sliceDirection));

            return slices.Select(a => ToParcel(parcel, a));
        }

        private static Parcel ToParcel(Parcel parent, IEnumerable<Vector2> child)
        {
            Contract.Requires(parent != null);
            Contract.Requires(child != null);

            Vector2[] points = child.ToArray();
            if (points.Area() < 0)
                Array.Reverse(points);

            //Build edges from this set of points
            Parcel.Edge[] edges = new Parcel.Edge[points.Length];
            for (int i = 0; i < points.Length; i++)
                edges[i] = new Parcel.Edge { Start = points[i], End = points[(i + 1) % points.Length], Resources = new string[0] };

            //Find egdes in parent which are coincident with edges of child
            //Copy road accessibility across
            foreach (var parentEdge in parent.Edges)
            {
                var parentDirection = Vector2.Normalize(parentEdge.End - parentEdge.Start);
                for (int j = 0; j < edges.Length; j++)
                {
                    var childEdge = edges[j];

                    var s = Math.Abs(new Ray2(parentEdge.Start, parentDirection).DistanceToPoint(childEdge.Start));
                    var e = Math.Abs(new Ray2(parentEdge.Start, parentDirection).DistanceToPoint(childEdge.End));

                    if (s < 0.01 && e < 0.01) //Pretty massive threshold, but it's only really 1cm, so close enough when we're talking about entire city blocks!
                    {
                        edges[j].Resources = parentEdge.Resources;
                        break;
                    }
                }
            }

            return new Parcel(edges);
        }

        internal static OABR FitOabb(Parcel parcel, float nonOptimalityChance, float maximumNonOptimality, Func<double> random)
        {
            Contract.Requires(parcel != null);
            Contract.Requires(random != null);

            //Generate a set of OABBs, order by size
            var oabbs = OABR.Fittings(parcel.Points()).OrderBy(a =>  a.Area).ToArray();

            //Now select an OABB from this list, with the first (smallest) being the most likely
            var selected = 0;
            for (var i = 1; i < oabbs.Length; i++)
            {
                //Do not allow the area ratio between optimal and selected to go over the non optimality limit
                if (oabbs[i].Area / (oabbs[0]).Area > maximumNonOptimality)
                    break;

                //We're not breaking the non optimality limit, so what's the chance of selecting this?
                var chance = Math.Pow(nonOptimalityChance, i);
                selected = random() < chance ? i : selected;
            }

            return oabbs[selected];
        }
        #endregion
    }
}
