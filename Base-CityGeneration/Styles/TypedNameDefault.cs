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
    }
}
