using System;
using System.Linq;
using System.Numerics;
using EpimetheusPlugins.Procedural;
using EpimetheusPlugins.Procedural.Utilities;
using EpimetheusPlugins.Scripts;
using Myre;
using Myre.Collections;
using Myre.Extensions;

namespace Base_CityGeneration.Elements.Building.Internals.VerticalFeatures.Stairs
{
    /// <summary>
    /// A stairwell which has stairs running up the outer walls.
    /// </summary>
    [Script("A9264DDE-8BE5-45B8-BD5F-5AEA8BC40142", "Spiral Staircase")]
    public class SpiralStaircase
        :ProceduralScript, IStairwell
    {
        public bool IsMajorFeature
        {
            get { return true; }
        }

        public int BottomFloorIndex { get; set; }
        public int TopFloorIndex { get; set; }
        public float FloorHeight { get; set; }

        public override bool Accept(Prism bounds, INamedDataProvider parameters)
        {
            //Todo: check that there are sufficient number of sufficient length walls to build a decent set of stairs
            return true;
        }

        public override void Subdivide(Prism bounds, ISubdivisionGeometry geometry, INamedDataCollection hierarchicalParameters)
        {
            //Get parameters
            string material = hierarchicalParameters.GetValue(new TypedName<string>("material"));
            var stairWidth = hierarchicalParameters.GetMaybeValue(new TypedName<float>("step_width")) ?? 1;
            var minStepDepth = hierarchicalParameters.GetMaybeValue(new TypedName<float>("step_depth_min")) ?? 0.45f;
            var maxStepUp = hierarchicalParameters.GetMaybeValue(new TypedName<float>("step_up_max")) ?? 0.35f;

            var inner = bounds.Footprint.Shrink(stairWidth).ToArray();
            var stairSections = bounds.Footprint.Sections(inner).ToArray();
            var sectionStepCounts = stairSections.Select(section =>
            {
                var along = Math.Min((section.B - section.A).Length(), (section.D - section.C).Length());
                var steps = section.IsCorner ? 1 : (int)Math.Floor(along / minStepDepth);
                return new
                {
                    Section = section,
                    Steps = steps
                };
            }).ToArray();

            //Count up how many sections we need to exceed the roof height, then shrink the step height down so this many sections perfectly fits the available height
            int totalSteps = 0;
            int sectionCount = 0;
            while ((totalSteps * maxStepUp) < bounds.Height)
                totalSteps += sectionStepCounts[(sectionCount++) % sectionStepCounts.Length].Steps;

            //The height of a single step
            float stepHeight = bounds.Height / totalSteps;

            float heightOffset = -bounds.Height / 2;
            for (int i = 0; i < sectionCount; i++)
            {
                var section = sectionStepCounts[i % sectionStepCounts.Length];
                PlaceSection(material, geometry, section.Section.C, section.Section.D, section.Section.B, section.Section.A, stepHeight, heightOffset, section.Steps);
                heightOffset += section.Steps * stepHeight;
            }

            SubdivideEmptyCentralSpace(new Prism(bounds.Height, inner), geometry, hierarchicalParameters);
        }

        /// <summary>
        /// Do something with the empty space in the middle of the spiral (by default nothing)
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="geometry"></param>
        /// <param name="hierarchicalParameters"></param>
        protected virtual void SubdivideEmptyCentralSpace(Prism bounds, ISubdivisionGeometry geometry, INamedDataCollection hierarchicalParameters)
        {
        }

        private static void PlaceSection(string material, ISubdivisionGeometry geometry, Vector2 side1Start, Vector2 side1End, Vector2 side2Start, Vector2 side2End, float stepHeight, float heightOffset, int steps)
        {
            Vector2 stepSide1 = (side1End - side1Start) / steps;
            Vector2 stepSide2 = (side2End - side2Start) / steps;

            Vector2 a = side1Start;
            Vector2 d = side2Start;

            // A single step is (top down):
            //
            // B - - - - - - C
            // |             |
            // A - - - - - - D
            //
            // A flight of steps is (side on):
            //
            // o-o
            // | |
            // o-o-o
            //   | |
            //   o-o-o
            //     | |
            //     o-o

            for (int i = 0; i < steps; i++)
            {
                Vector2 b = a + stepSide1;
                Vector2 c = d + stepSide2;

                var step = geometry
                    .CreatePrism(material, new Vector2[] { a, b, c, d }, stepHeight)
                    .Translate(new Vector3(0, heightOffset + i * stepHeight + stepHeight * 0.5f, 0));
                geometry.Union(step);

                //Shift the end of this step to be the start of the next step
                a = b;
                d = c;
            }
        }
    }
}
