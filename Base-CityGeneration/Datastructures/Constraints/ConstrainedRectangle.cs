using Cassowary;

namespace Base_CityGeneration.Datastructures.Constraints
{
    /// <summary>
    /// A rectangle with sizes and positions specified by linear constraints
    /// </summary>
    public class ConstrainedRectangle
    {
        /// <summary>
        /// The constraint for the left of this rectangle
        /// </summary>
        public ClVariable LeftVariable { get; private set; }
        public float Left
        {
            get { return (float) LeftVariable.Value; }
        }

        /// <summary>
        /// The constraint for the right of this rectangle
        /// </summary>
        public ClVariable RightVariable { get; private set; }
        public float Right
        {
            get { return (float)RightVariable.Value; }
        }

        /// <summary>
        /// The constraint for the top of this rectangle
        /// </summary>
        public ClVariable TopVariable { get; private set; }
        public float Top
        {
            get { return (float)TopVariable.Value; }
        }

        /// <summary>
        /// The constraint for the bottom of this rectangle
        /// </summary>
        public ClVariable BottomVariable { get; private set; }
        public float Bottom
        {
            get { return (float)BottomVariable.Value; }
        }

        /// <summary>
        /// The constraint for the width of this rectangle
        /// </summary>
        public ClVariable WidthVariable { get; private set; }
        public float Width
        {
            get { return (float)WidthVariable.Value; }
        }

        /// <summary>
        /// The constraint for the height of this rectangle
        /// </summary>
        public ClVariable HeightVariable { get; private set; }
        public float Height
        {
            get { return (float)HeightVariable.Value; }
        }

        /// <summary>
        /// The constraint for the center of this rectangle on the x axis
        /// </summary>
        public ClVariable CenterXVariable { get; private set; }
        public float CenterX
        {
            get { return (float)CenterXVariable.Value; }
        }

        /// <summary>
        /// The constraint for the center of this rectangle on the y axis
        /// </summary>
        public ClVariable CenterYVariable { get; private set; }
        public float CenterY
        {
            get { return (float)CenterYVariable.Value; }
        }

        /// <summary>
        /// The current rectangle which this set of constraints is solved to
        /// </summary>
        public RectangleF Rectangle
        {
            get { return new RectangleF(Left, Top, Width, Height); }
        }

        public ConstrainedRectangle(ClSimplexSolver solver, Range left = null, Range right = null, Range top = null, Range bottom = null, Range width = null, Range height = null, Range centerX = null, Range centerY = null)
        {
            LeftVariable = (left ?? new Range()).CreateVariable(solver);
            RightVariable = (right ?? new Range()).CreateVariable(solver);
            TopVariable = (top ?? new Range()).CreateVariable(solver);
            BottomVariable = (bottom ?? new Range()).CreateVariable(solver);

            WidthVariable = (width ?? new Range()).CreateVariable(solver);
            HeightVariable = (height ?? new Range()).CreateVariable(solver);

            CenterXVariable = (centerX ?? new Range()).CreateVariable(solver);
            CenterYVariable = (centerY ?? new Range()).CreateVariable(solver);

// ReSharper disable CompareOfFloatsByEqualityOperator
            solver.AddConstraint(LeftVariable, WidthVariable, RightVariable, (l, w, r) => l + w == r);
            solver.AddConstraint(BottomVariable, HeightVariable, TopVariable, (b, h, t) => b + h == t);
            solver.AddConstraint(CenterXVariable, LeftVariable, WidthVariable, (cx, l, w) => l + w / 2 == cx);
            solver.AddConstraint(CenterYVariable, BottomVariable, HeightVariable, (cy, b, h) => b + h / 2 == cy);
// ReSharper restore CompareOfFloatsByEqualityOperator
        }
    }
}
