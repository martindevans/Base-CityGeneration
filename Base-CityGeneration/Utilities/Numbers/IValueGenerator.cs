using System;

namespace Base_CityGeneration.Utilities.Numbers
{
    public interface IValueGenerator
    {
        float SelectFloatValue(Func<double> random);

        int SelectIntValue(Func<double> random);
    }

    internal interface IValueGeneratorContainer
    {
        IValueGenerator Unwrapped { get; set; }

        IValueGenerator Unwrap();
    }
}
