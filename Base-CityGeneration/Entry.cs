using EpimetheusPlugins.Scripts;
using Ninject;

namespace Base_CityGeneration
{
    /// <summary>
    /// Entry point to the plugin (contains a method which is called when the plugin is loaded into the engine)
    /// </summary>
    [Script("3213818C-1687-432C-9F60-59C6ACA59BD9", "Base-CityGeneration Entry Point")]
    public class Entry
        : IPluginEntryPoint
    {
        /// <summary>
        /// Entry point, called when plugin is loaded into the engine
        /// </summary>
        /// <param name="kernel"></param>
        public void Loaded(IKernel kernel)
        {
        }
    }

    /// <summary>
    /// Entry point of the plugin when built and run as a standalone application
    /// </summary>
    public static class Program
    {
        public static void Main(string[] args)
        {
        }
    }
}
