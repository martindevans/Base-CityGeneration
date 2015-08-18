using Myre.Collections;
using System;

namespace Base_CityGeneration.Utilities.Numbers
{
    public interface IValueGenerator
    {
        float SelectFloatValue(Func<double> random, INamedDataCollection data);

        int SelectIntValue(Func<double> random, INamedDataCollection data);
    }
}
