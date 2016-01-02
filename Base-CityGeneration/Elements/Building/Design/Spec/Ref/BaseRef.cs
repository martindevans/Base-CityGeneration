using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace Base_CityGeneration.Elements.Building.Design.Spec.Ref
{
    public abstract class BaseRef
    {
        /// <summary>
        /// After we have produced a set of pairs, this specifies how to filter them to a smaller set
        /// </summary>
        protected RefFilter Filter { get; private set; }

        /// <summary>
        /// If we're in non overlapping mode the pairs are split into sets of overlapping pairs, and then each pair has the filter mode applies separately
        /// </summary>
        public bool NonOverlapping { get; private set; }

        /// <summary>
        /// Whether we allow pairs to be form of the same floor at the top and bottom
        /// </summary>
        public bool Inclusive { get; private set; }

        protected BaseRef(RefFilter filter, bool nonOverlapping, bool inclusive)
        {
            Filter = filter;
            NonOverlapping = nonOverlapping;
            Inclusive = inclusive;
        }

        public IEnumerable<FloorSelection> Match(IList<FloorSelection> floors, int? startIndex)
        {
            return MatchImpl(floors, startIndex);
        }

        public IEnumerable<KeyValuePair<FloorSelection, FloorSelection>> MatchFrom(IList<FloorSelection> floors, BaseRef start, IEnumerable<FloorSelection> selectedByStart)
        {
            Contract.Requires(floors != null);
            Contract.Requires(start != null);
            Contract.Requires(selectedByStart != null);
            Contract.Ensures(Contract.Result<IEnumerable<KeyValuePair<FloorSelection, FloorSelection>>>() != null);

            //Select all matching pairs
            var selected = start.FilterByMode(selectedByStart.SelectMany(a => {

                var matches = MatchImpl(floors, a.Index)
                    .Select(b => new KeyValuePair<FloorSelection, FloorSelection>(a, b))
                    .Where(p => Inclusive || p.Key.Index != p.Value.Index);

                return FilterByMode(matches);
            }));

            //If we don't care about overlaps we're done, good to go
            if (!NonOverlapping)
                return selected;

            //We need to reject overlaps
            //First group into overlapping sets
            var sets = new List<List<KeyValuePair<FloorSelection, FloorSelection>>>();
            foreach (var kvp in selected)
            {
                var set = sets.FirstOrDefault(s => s.Any(a => a.Key.Index <= kvp.Value.Index && a.Value.Index >= kvp.Key.Index));
                if (set != null)
                    set.Add(kvp);
                else
                    sets.Add(new List<KeyValuePair<FloorSelection, FloorSelection>> { kvp });
            }

            //Now filter *each set* by the filter mode
            return sets.SelectMany(FilterByMode);
        }

        private IEnumerable<KeyValuePair<FloorSelection, FloorSelection>> FilterByMode(IEnumerable<KeyValuePair<FloorSelection, FloorSelection>> zipped)
        {
            Contract.Requires(zipped != null, "zipped");

            switch (Filter)
            {
                case RefFilter.All:
                    return zipped;
                case RefFilter.First:
                    return zipped.Take(1);
                case RefFilter.Second:
                    return zipped.Skip(1).Take(1);
                case RefFilter.Last:
                    return zipped.Reverse().Take(1).ToArray();
                case RefFilter.Shortest:
                    if (!zipped.Any())
                        return new KeyValuePair<FloorSelection, FloorSelection>[0];
                    return new[] { zipped.Aggregate((a, b) => Math.Abs(a.Key.Index - a.Value.Index) > Math.Abs(b.Key.Index - b.Value.Index) ? b : a) };
                case RefFilter.Longest:
                    if (!zipped.Any())
                        return new KeyValuePair<FloorSelection, FloorSelection>[0];
                    return new[] { zipped.Aggregate((a, b) => Math.Abs(a.Key.Index - a.Value.Index) > Math.Abs(b.Key.Index - b.Value.Index) ? a : b) };
                case RefFilter.SingleOrNone:
                    if (zipped.Skip(1).Any())
                        return new KeyValuePair<FloorSelection, FloorSelection>[0];
                    return zipped.Take(1);
                case RefFilter.SingleOrFail:
                    if (zipped.Skip(1).Any())
                        throw new DesignFailedException("Multiple matches found by SingleOrFail filter");
                    return zipped.Take(1);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected abstract IEnumerable<FloorSelection> MatchImpl(IList<FloorSelection> floors, int? startIndex);

        protected static IEnumerable<FloorSelection> Prefilter(IEnumerable<FloorSelection> floors, int? startIndex, SearchDirection direction = SearchDirection.Down)
        {
            Contract.Requires(floors != null);
            Contract.Ensures(Contract.Result<IEnumerable<FloorSelection>>() != null);

            switch (direction)
            {
                case SearchDirection.Up:
                    return floors.Where(a => !startIndex.HasValue || a.Index >= startIndex.Value).OrderBy(a => a.Index);
                case SearchDirection.Down:
                    return floors.Where(a => !startIndex.HasValue || a.Index <= startIndex.Value).OrderByDescending(a => a.Index);
                default:
                    throw new ArgumentException("Unknown search direction");
            }
        }

        internal abstract class BaseContainer
        {
            // Making protected would break serialization
            // ReSharper disable MemberCanBeProtected.Global
            public RefFilter? Filter { get; set; }

            public bool Inclusive { get; set; }

            public bool NonOverlapping { get; set; }
            // ReSharper restore MemberCanBeProtected.Global

            public abstract BaseRef Unwrap();
        }
    }

    public enum RefFilter
    {
        /// <summary>
        /// When there are multiple choices, take them all
        /// </summary>
        All,

        /// <summary>
        /// Take the first created element
        /// </summary>
        First,

        /// <summary>
        /// Take the second created element (useful for skipping over matches with self)
        /// </summary>
        Second,

        /// <summary>
        /// Take the last created element
        /// </summary>
        Last,

        /// <summary>
        /// Take the shortest element
        /// </summary>
        Shortest,

        /// <summary>
        /// Take the longest element
        /// </summary>
        Longest,

        /// <summary>
        /// If only one option was generated, take that. Otherwise take none
        /// </summary>
        SingleOrNone,

        /// <summary>
        /// If only one option was generated, take that. Otherwise take fail (cancel entire building)
        /// </summary>
        SingleOrFail,
    }
}
