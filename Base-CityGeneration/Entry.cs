using System;
using System.Numerics;
using CGAL_StraightSkeleton_Dotnet;
using EpimetheusPlugins.Scripts;
using Ninject;

namespace Base_CityGeneration
{
    [Script("3213818C-1687-432C-9F60-59C6ACA59BD9", "Base-CityGeneration Entry Point")]
    public class Entry
        : IPluginEntryPoint
    {
        public void Loaded(IKernel kernel)
        {
        }
    }

    public static class Program
    {
        public static void Main(string[] args)
        {
            var ss = StraightSkeleton.Generate(new Vector2[] {
                new Vector2(20, 10),
                new Vector2(20, -10),
                new Vector2(-20, -10),
                new Vector2(-20, 10),
            });

            Console.WriteLine(ss);
        }
    }
}
