using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using EpimetheusPlugins.Procedural;
using Myre;
using Myre.Collections;

namespace Base_CityGeneration.Styles
{
    public static class Materials
    {
        public static readonly TypedNameDefault<string> DefaultMaterialName = new TypedNameDefault<string>("material", null);

        public static string DefaultMaterial(this INamedDataCollection provider, Func<double> random, params string[] possibilities)
        {
            Contract.Requires(provider != null, "provider != null");
            Contract.Requires(random != null, "random != null");
            Contract.Requires(possibilities != null, "possibilities != null");

            return DefaultMaterial(provider, random, DefaultMaterialName, possibilities);
        }

        public static string DefaultMaterial(this INamedDataCollection provider, Func<double> random, TypedName<string> name, params string[] possibilities)
        {
            Contract.Requires(provider != null, "provider != null");
            Contract.Requires(random != null, "random != null");
            Contract.Requires(possibilities != null, "possibilities != null");

            //Select a random value from the possibilities
            var generated = possibilities.Length == 0 ? null : possibilities[random.RandomInteger(0, possibilities.Length - 1)];

            return provider.DetermineHierarchicalValue(name, oldValue =>
            {
                //If no possibilities were provided, everything is valid!
                if (possibilities.Length == 0)
                    return oldValue;

                //Use the old value if it is one of the allowed possibilities
                if (((IList<string>)possibilities).Contains(oldValue))
                    return oldValue;

                //Otherwise generate a new value (from the range of allowed possibilities)
                return generated;
            }, () => generated);
        }
    }
}
