﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Myre;
using Myre.Collections;

namespace Base_CityGeneration.Styles
{
    /// <summary>
    /// A strongly typed name for a value, along with a default value to use
    /// </summary>
    /// <typeparam name="T"></typeparam>
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
            Contract.Requires(tnd != null);

            return new TypedName<T>(tnd._name);
        }
    }

    public static class INamedDataProviderExtensions
    {
        /// <summary>
        /// Get the value stored with this name and type and use the default if no value is found
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="provider"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static T GetValue<T>(this INamedDataProvider provider, TypedNameDefault<T> name)
        {
            Contract.Requires(provider != null);
            Contract.Requires(name != null);

            T value;
            if (provider.TryGetValue<T>(name, out value))
                return value;

            return name.Default;
        }

        /// <summary>
        /// Determine a value by deriving it from a previously calculated value and then store the result
        /// </summary>
        /// <typeparam name="T">The type of the value</typeparam>
        /// <param name="provider">Provider for previously defined value as well as the target to store the new value in</param>
        /// <param name="random">A random number generator (supply a biased generator to bias the values generated)</param>
        /// <param name="interpolate">A function which interpolates between values</param>
        /// <param name="name">the name of this value</param>
        /// <param name="minName">The name of the lower bound to use</param>
        /// <param name="maxName">The name of the upper bound to use</param>
        /// <param name="min">The minimum value to use (or null, to not set a minimum bound)</param>
        /// <param name="max">The maximum value to use (or null, to not set a maximum bound)</param>
        /// <returns></returns>
        public static T DetermineHierarchicalValue<T>(this INamedDataCollection provider, Func<double> random, Func<T, T, float, T> interpolate, TypedName<T> name, TypedNameDefault<T> minName, TypedNameDefault<T> maxName, T? min = null, T? max = null) where T : struct, IComparable<T>, IEquatable<T>
        {
            Contract.Requires(provider != null);
            Contract.Requires(random != null);
            Contract.Requires(interpolate != null);
            Contract.Requires(minName != null);
            Contract.Requires(maxName != null);

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

        /// <summary>
        /// Determine a value by deriving it from a previously calculated value and then store the result
        /// </summary>
        /// <typeparam name="T">The type of the value</typeparam>
        /// <param name="provider">Provider for previously defined value as well as the target to store the new value in</param>
        /// <param name="name">The name of the value</param>
        /// <param name="update">Function to derive a new value from an old one</param>
        /// <param name="generate">Function to generate a new value if no old one existed</param>
        /// <returns>The determined value</returns>
        public static T DetermineHierarchicalValue<T>(this INamedDataCollection provider, TypedName<T> name, Func<T, T> update, Func<T> generate) where T : IEquatable<T>
        {
            Contract.Requires(provider != null);
            Contract.Requires(update != null);
            Contract.Requires(generate != null);

            T value;
            if (provider.TryGetValue<T>(name, out value))
            {
                var oldValue = value;
                value = update(oldValue);

                if (!EqualityComparer<T>.Default.Equals(oldValue, value))
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
