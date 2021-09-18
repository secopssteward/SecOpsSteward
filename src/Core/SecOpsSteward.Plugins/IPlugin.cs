using System.Collections.Generic;
using System.Threading.Tasks;
using SecOpsSteward.Plugins.Configurable;

namespace SecOpsSteward.Plugins
{
    public interface IPlugin : IConfigurableObject, IHasDescriptiveMetadata
    {
        /// <summary>
        ///     RBAC Requirements for plugin (used in grant/revoke)
        /// </summary>
        IEnumerable<PluginRbacRequirements> RbacRequirements { get; }

        /// <summary>
        ///     Execute the plugin with the loaded configuration
        /// </summary>
        /// <param name="previousOutput">Previous plugin output(s)</param>
        /// <returns>Plugin result</returns>
        Task<PluginOutputStructure> Execute(PluginOutputStructure previousOutput);
    }

    /// <summary>
    ///     Plugin which takes a configuration object
    /// </summary>
    /// <typeparam name="TConfiguration">Plugin configuration type</typeparam>
    public interface IPlugin<TConfiguration> : IPlugin
        where TConfiguration : IConfigurableObjectConfiguration
    {
        /// <summary>
        ///     Plugin configuration
        /// </summary>
        TConfiguration Configuration { get; set; }
    }
}