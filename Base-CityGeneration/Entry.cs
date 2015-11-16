using System;
using System.Numerics;
using EpimetheusPlugins.Scripts;
using Ninject;

namespace Base_CityGeneration
{
    public class Entry
        : IPluginEntryPoint
    {
        public void Loaded(IKernel kernel)
        {
            Console.WriteLine("Loaded(IKernel) Base-CityGeneration");

            CGAL_StraightSkeleton_Dotnet.StraightSkeleton.Generate(new Vector2[] {
                new Vector2(10, 10),
                new Vector2(10, -10),
                new Vector2(-10, -10),
                new Vector2(-10, 10),
            });
        }
    }
}
