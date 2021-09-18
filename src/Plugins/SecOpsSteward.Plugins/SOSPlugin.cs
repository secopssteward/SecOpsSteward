using System.Collections.Generic;
using System.Threading.Tasks;
using SecOpsSteward.Plugins.Configurable;

namespace SecOpsSteward.Plugins
{
    public abstract class SOSPlugin<TConfiguration> : IPlugin<TConfiguration>
        where TConfiguration : IConfigurableObjectConfiguration
    {
        /// <summary>
        ///     Plugin configuration
        /// </summary>
        public TConfiguration Configuration { get; set; }

        /// <summary>
        ///     Execute the plugin with the loaded configuration
        /// </summary>
        /// <param name="previousOutput">Previous plugin output(s)</param>
        /// <returns>Plugin result</returns>
        public abstract Task<PluginOutputStructure> Execute(PluginOutputStructure previousOutput);

        /// <summary>
        ///     RBAC Requirements for plugin (used in grant/revoke)
        /// </summary>
        public virtual IEnumerable<PluginRbacRequirements> RbacRequirements { get; } =
            new List<PluginRbacRequirements>();
    }
}