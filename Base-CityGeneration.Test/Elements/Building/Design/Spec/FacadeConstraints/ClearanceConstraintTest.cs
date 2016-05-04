using System.Collections.Generic;
using System.Numerics;
using Base_CityGeneration.Elements.Building.Design;
using Base_CityGeneration.Elements.Building.Design.Spec.FacadeConstraints;
using EpimetheusPlugins.Scripts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Base_CityGeneration.Test.Elements.Building.Design.Spec.FacadeConstraints
{
    [TestClass]
    public class ClearanceConstraintTest
    {
        readonly ClearanceConstraint _constraint = new ClearanceConstraint(100);

        [TestMethod]
        public void AssertThat_OppositeEdge_IsClear()
        {
            var result = _constraint.Check(new FloorSelection("", new KeyValuePair<string, string>[0], new ScriptReference(typeof(TestScript)), 0, 0), new BuildingSideInfo[] {
                new BuildingSideInfo(new Vector2(10, 20), new Vector2(10, -20), new BuildingSideInfo.NeighbourInfo[]{
                    new BuildingSideInfo.NeighbourInfo(0, 1, 100, null)
                }),
            }, new Vector2(0, 0), new Vector2(0, 10), 0, 1);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void AssertThat_MatchingEdge_IsNotClear()
        {
            var result = _constraint.Check(new FloorSelection("", new KeyValuePair<string, string>[0], new ScriptReference(typeof(TestScript)), 0), new BuildingSideInfo[] {
                new BuildingSideInfo(new Vector2(-10, 20), new Vector2(-10, -20), new BuildingSideInfo.NeighbourInfo[]{
                    new BuildingSideInfo.NeighbourInfo(0, 1, 100, null)
                }),
            }, new Vector2(0, 0), new Vector2(0, 10), 0, 1);

            Assert.IsFalse(result);
        }
    }
}
