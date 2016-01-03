using System.Diagnostics.Contracts;
using Cassowary;

namespace Base_CityGeneration.Datastructures.Constraints
{
    /// <summary>
    /// A rectangle with sizes and positions specified by linear constraints
    /// </summary>
    public class ConstrainedRectangle
    {
        private readonly ClVariable _leftVar;
        /// <summary>
        /// The constraint for the left of this rectangle
        /// </summary>
        public ClVariable LeftVariable
        {
            get
            {
                Contract.Ensures(Contract.Result<ClVariable>() != null);
                return _leftVar;
            }
        }
        public float Left
        {
            get { return (float) LeftVariable.Value; }
        }

        private readonly ClVariable _rightVar;
        /// <summary>
        /// The constraint for the right of this rectangle
        /// </summary>
        public ClVariable RightVariable
        {
            get
            {
                Contract.Ensures(Contract.Result<ClVariable>() != null);
                return _rightVar;
            }
        }
        public float Right
        {
            get { return (float)RightVariable.Value; }
        }

        private readonly ClVariable _topVar;
        /// <summary>
        /// The constraint for the top of this rectangle
        /// </summary>
        public ClVariable TopVariable
        {
            get
            {
                Contract.Ensures(Contract.Result<ClVariable>() != null);
                return _topVar;
            }
        }
        public float Top
        {
            get { return (float)TopVariable.Value; }
        }

        private readonly ClVariable _botVar;
        /// <summary>
        /// The constraint for the bottom of this rectangle
        /// </summary>
        public ClVariable BottomVariable
        {
            get
            {
                Contract.Ensures(Contract.Result<ClVariable>() != null);
                return _botVar;
            }
        }
        public float Bottom
        {
            get { return (float)BottomVariable.Value; }
        }

        private readonly ClVariable _widthVar;
        /// <summary>
        /// The constraint for the width of this rectangle
        /// </summary>
        public ClVariable WidthVariable
        {
            get
            {
                Contract.Ensures(Contract.Result<ClVariable>() != null);
                return _widthVar;
            }
        }
        public float Width
        {
            get { return (float)WidthVariable.Value; }
        }

        private readonly ClVariable _heightVar;
        /// <summary>
        /// The constraint for the height of this rectangle
        /// </summary>
        public ClVariable HeightVariable
        {
            get
            {
                Contract.Ensures(Contract.Result<ClVariable>() != null);
                return _heightVar;
            }
        }
        public float Height
        {
            get { return (float)HeightVariable.Value; }
        }

        private readonly ClVariable _cxVar;
        /// <summary>
        /// The constraint for the center of this rectangle on the x axis
        /// </summary>
        public ClVariable CenterXVariable
        {
            get
            {
                Contract.Ensures(Contract.Result<ClVariable>() != null);
                return _cxVar;
            }
        }
        public float CenterX
        {
            get { return (float)CenterXVariable.Value; }
        }

        private readonly ClVariable _cyVar;
        /// <summary>
        /// The constraint for the center of this rectangle on the y axis
        /// </summary>
        public ClVariable CenterYVariable
        {
            get
            {
                Contract.Ensures(Contract.Result<ClVariable>() != null);
                return _cyVar;
            }
        }
        public float CenterY
        {
            get { return (float)CenterYVariable.Value; }
        }

        private readonly ClSimplexSolver _solver;

        public ConstrainedRectangle(ClSimplexSolver solver, Range left = null, Range right = null, Range top = null, Range bottom = null, Range width = null, Range height = null, Range centerX = null, Range centerY = null)
        {
            Contract.Requires(solver != null);

            _solver = solver;

            _leftVar = (left ?? new Range()).CreateVariable(solver);
            _rightVar = (right ?? new Range()).CreateVariable(solver);
            _topVar = (top ?? new Range()).CreateVariable(solver);
            _botVar = (bottom ?? new Range()).CreateVariable(solver);

            _widthVar = (width ?? new Range()).CreateVariable(solver);
            _heightVar = (height ?? new Range()).CreateVariable(solver);

            _cxVar = (centerX ?? new Range()).CreateVariable(solver);
            _cyVar = (centerY ?? new Range()).CreateVariable(solver);

// ReSharper disable CompareOfFloatsByEqualityOperator
            solver.AddConstraint(LeftVariable, WidthVariable, RightVariable, (l, w, r) => l + w == r);
            solver.AddConstraint(BottomVariable, HeightVariable, TopVariable, (b, h, t) => b + h == t);
            solver.AddConstraint(CenterXVariable, LeftVariable, WidthVariable, (cx, l, w) => l + w / 2 == cx);
            solver.AddConstraint(CenterYVariable, BottomVariable, HeightVariable, (cy, b, h) => b + h / 2 == cy);
// ReSharper restore CompareOfFloatsByEqualityOperator
        }

        [ContractInvariantMethod]
        private void ObjectInvariant()
        {
            Contract.Invariant(_leftVar != null);
            Contract.Invariant(_rightVar != null);
            Contract.Invariant(_botVar != null);
            Contract.Invariant(_topVar != null);

            Contract.Invariant(_widthVar != null);
            Contract.Invariant(_heightVar != null);

            Contract.Invariant(_cxVar != null);
            Contract.Invariant(_cyVar != null);

            Contract.Invariant(_solver != null);
        }

        public ConstrainedRectangle AspectRatio(float aspectRatio)
        {
            Contract.Ensures(Contract.Result<ConstrainedRectangle>() != null);

// ReSharper disable CompareOfFloatsByEqualityOperator
            _solver.AddConstraint(WidthVariable, HeightVariable, (w, h) => w == h * aspectRatio);
// ReSharper restore CompareOfFloatsByEqualityOperator

            return this;
        }

        public ConstrainedRectangle AspectRatio(Range aspectRange)
        {
            Contract.Requires(aspectRange != null);
            Contract.Ensures(Contract.Result<ConstrainedRectangle>() != null);

            if (aspectRange.Min.HasValue)
            {
                var m = aspectRange.Min.Value;
                _solver.AddConstraint(WidthVariable, HeightVariable, (w, h) => w >= h * m);
            }
            if (aspectRange.Max.HasValue)
            {
                var m = aspectRange.Max.Value;
                _solver.AddConstraint(WidthVariable, HeightVariable, (w, h) => w <= h * m);
            }

            return this;
        }
    }
}
