using System;
using System.Diagnostics.Contracts;
using Cassowary;

namespace Base_CityGeneration.Datastructures.Constraints
{
    public class Range
    {
        public double? Min { get; private set; }
        public double? Max { get; private set; }

        public Range(double? min, double? max)
        {
            Min = min;
            Max = max;
        }

        public Range()
            : this(null, null)
        {
        }

        internal ClVariable CreateVariable(ClSimplexSolver solver)
        {
            Contract.Requires(solver != null);
            Contract.Ensures(Contract.Result<ClVariable>() != null);

            var variable = new ClVariable(Guid.NewGuid().ToString());

            if (Min.HasValue)
            {
                var min = Min.Value;
                solver.AddConstraint(variable, a => a >= min);
            }
            if (Max.HasValue)
            {
                var max = Max.Value;
                solver.AddConstraint(variable, a => a <= max);
            }

            return variable;
        }
    }
}
