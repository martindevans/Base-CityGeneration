
using System;
using System.Collections.Generic;
using System.Linq;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Selection.Spec.Ref
{
    public abstract class BaseRef
    {
        protected RefFilter Filter { get; private set; }

        public bool NonOverlapping { get; private set; }

        protected BaseRef(RefFilter filter, bool nonOverlapping)
        {
            Filter = filter;
            NonOverlapping = nonOverlapping;
        }

        public IEnumerable<FloorSelection> Match(int basements, FloorSelection[] floors, int? startIndex)
        {
            return MatchImpl(basements, floors, startIndex);
        }

        public IEnumerable<KeyValuePair<FloorSelection, FloorSelection>> MatchFrom(int basements, FloorSelection[] floors, BaseRef start, IEnumerable<FloorSelection> selectedByStart)
        {
            //Select all matching pairs
            var selected = start.FilterByMode(selectedByStart.SelectMany(a => {

                var matches = MatchImpl(basements, floors, Array.IndexOf(floors, a))
                    .Select(b => new KeyValuePair<FloorSelection, FloorSelection>(a, b))
                    .Where(b => b.Key.Index != b.Value.Index);

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
                    throw new NotImplementedException();
                case RefFilter.Longest:
                    throw new NotImplementedException();
                case RefFilter.SingleOrNone:
                    if (zipped.Skip(1).Any())
                        return new KeyValuePair<FloorSelection, FloorSelection>[0];
                    return zipped.Take(1);
                case RefFilter.SingleOrFail:
                    throw new NotImplementedException();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected abstract IEnumerable<FloorSelection> MatchImpl(int basements, FloorSelection[] floors, int? startIndex);

        internal abstract class BaseContainer
        {
            // Making protected would break serialization
            // ReSharper disable MemberCanBeProtected.Global
            public RefFilter? Filter { get; set; }

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
