using Myre.Collections;
using System;
using System.Diagnostics.Contracts;

namespace Base_CityGeneration.Utilities.Numbers
{
    [ContractClass(typeof(IValueGeneratorContracts))]
    public interface IValueGenerator
    {
        float MaxValue { get; }
        float MinValue { get; }

        float SelectFloatValue(Func<double> random, INamedDataCollection data);
    }

    [ContractClassFor(typeof(IValueGenerator))]
    internal abstract class IValueGeneratorContracts
        : IValueGenerator
    {

        public float MaxValue
        {
            get { return default(float); }
        }

        public float MinValue
        {
            get { return default(float); }
        }

        public float SelectFloatValue(Func<double> random, INamedDataCollection data)
        {
            Contract.Requires(random != null);
            Contract.Requires(data != null);

            return default(float);
        }
    }

    // ReSharper disable once InconsistentNaming
    [ContractClass(typeof(IValueGeneratorContainerContracts))]
    internal abstract class IValueGeneratorContainer
        : IUnwrappable<IValueGenerator>
    {
        private IValueGenerator Unwrapped { get; set; }

        public IValueGenerator Unwrap()
        {
            Contract.Ensures(Contract.Result<IValueGenerator>() != null);

            if (Unwrapped == null)
                Unwrapped = UnwrapImpl();
            return Unwrapped;
        }

        protected abstract IValueGenerator UnwrapImpl();

        public static explicit operator IValueGeneratorContainer(float v)
        {
            return new ConstantValue.Container { Value = v };
        }

        public static explicit operator IValueGeneratorContainer(double v)
        {
            return new ConstantValue.Container { Value = (float)v };
        }

        public static explicit operator IValueGeneratorContainer(int v)
        {
            return new ConstantValue.Container { Value = v };
        }

        public static IValueGenerator FromObject(object v, object defaultValue = null)
        {
            Contract.Ensures(Contract.Result<IValueGenerator>() != null);

            //If we've been passed a container just unwrap that
            var container = v as IValueGeneratorContainer;
            if (container != null)
                return container.Unwrap();

            //Maybe we've actually been handed a generator directly? If so, use that
            var generator = v as IValueGenerator;
            if (generator != null)
                return generator;

            if (v == null)
            {
                if (defaultValue != null)
                    return FromObject(defaultValue);
                else
                    throw new ArgumentException("Value is null (and no default value was provided", "v");
            }

            var f = Convert.ToSingle(v);
            return ((IValueGeneratorContainer)f).Unwrap();
        }
    }

    [ContractClassFor(typeof(IValueGeneratorContainer))]
    internal abstract class IValueGeneratorContainerContracts
        : IValueGeneratorContainer
    {
        protected override IValueGenerator UnwrapImpl()
        {
            Contract.Ensures(Contract.Result<IValueGenerator>() != null);

            return default(IValueGenerator);
        }
    }
}
