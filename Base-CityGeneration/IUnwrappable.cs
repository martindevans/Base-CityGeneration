using System;
using Myre.Collections;

namespace Base_CityGeneration
{
    internal interface IUnwrappable<out T>
    {
        T Unwrap();
    }
}
