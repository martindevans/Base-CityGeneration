﻿using System;
using Myre;
using Myre.Collections;

namespace Base_CityGeneration.Styles
{
    public class TypedNameDefault<T>
    {
        private readonly string _name;
        private readonly T _value;

        public T Default
        {
            get { return _value; }
        }

        public TypedNameDefault(string name, T defaultValue)
        {
            _name = name;
            _value = defaultValue;
        }

        public static implicit operator TypedName<T>(TypedNameDefault<T> tnd)
        {
            return new TypedName<T>(tnd._name);
        }
    }

    public static class INamedDataProviderExtensions
    {
        public static T GetValue<T>(this INamedDataProvider provider, TypedNameDefault<T> name)
        {
            T value;
            if (provider.TryGetValue<T>(name, out value))
                return value;

            return name.Default;
        }

        public static T DetermineHierarchicalValue<T>(this INamedDataCollection provider, Func<double> random, Func<T, T, float, T> interpolate, TypedName<T> name, TypedNameDefault<T> minName, TypedNameDefault<T> maxName, T? min = null, T? max = null) where T : struct, IComparable<T>, IEquatable<T>
        {
            return DetermineHierarchicalValue<T>(provider, name, oldValue =>
            {
                //Take an existing value and restrict it into the given range

                var value = oldValue;
                if (min.HasValue)
                    value = min.Value.CompareTo(value) > 0 ? min.Value : value;
                if (max.HasValue)
                    value = max.Value.CompareTo(value) < 0 ? max.Value : value;

                return value;
            }, () =>
            {
                //No existing value, generate a new one (in the given range)

                //Select the *maximum* of the two minimums
                var minHierarchicalValue = provider.GetValue(minName);
                var minValue = min.HasValue ? (min.Value.CompareTo(minHierarchicalValue) > 0 ? min.Value : minHierarchicalValue) : minHierarchicalValue;

                //Select the *minimum* of the two maximums
                var maxHierarchicalValue = provider.GetValue(maxName);
                var maxValue = max.HasValue ? (max.Value.CompareTo(maxHierarchicalValue) > 0 ? max.Value : maxHierarchicalValue) : maxHierarchicalValue;

                //Determine a value in the given range
                return interpolate(minValue, maxValue, (float)random());
            });
        }

        public static T DetermineHierarchicalValue<T>(this INamedDataCollection provider, TypedName<T> name, Func<T, T> update, Func<T> generate) where T : IEquatable<T>
        {
            T value;
            if (provider.TryGetValue<T>(name, out value))
            {
                var oldValue = value;
                value = update(oldValue);

                if (!oldValue.Equals(value))
                    provider.Set<T>(name, value);
            }
            else
            {
                value = generate();
                provider.Set<T>(name, value);
            }

            return value;
        }
    }
}
