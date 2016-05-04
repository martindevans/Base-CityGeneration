using System.Numerics;
using EpimetheusPlugins.Procedural;
using EpimetheusPlugins.Scripts;
using Myre.Collections;

namespace Base_CityGeneration.TestHelpers.Scripts
{
    public class TestRoot
        : ProceduralRoot
    {
        private readonly ScriptReference _script;
        private readonly Prism _prism;

        public TestRoot(ScriptReference script, Prism prism)
        {
            _script = script;
            _prism = prism;
        }

        public override void Subdivide(Prism bounds, ISubdivisionGeometry geometry, INamedDataCollection hierarchicalParameters)
        {
            CreateChild(_prism, Quaternion.Identity, Vector3.Zero, _script);
        }
    }
}
