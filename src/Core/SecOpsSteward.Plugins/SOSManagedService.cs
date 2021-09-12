using SecOpsSteward.Plugins.Configurable;
using SecOpsSteward.Plugins.Discovery;
using SecOpsSteward.Plugins.WorkflowTemplates;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SecOpsSteward.Plugins
{
    /// <summary>
    /// Package for all plugins associated with a managed service
    /// </summary>
    public abstract class SOSManagedService<TConfiguration> : IManagedServicePackage<TConfiguration>
        where TConfiguration : IConfigurableObjectConfiguration
    {
        /// <summary>
        /// Managed service configuration
        /// </summary>
        public TConfiguration Configuration { get; set; }

        /// <summary>
        /// Role this managed service plays in systems
        /// </summary>
        public abstract ManagedServiceRole Role { get; }

        /// <summary>
        /// Templates which work with this managed service
        /// </summary>
        public abstract List<WorkflowTemplateDefinition> Templates { get; }

        /// <summary>
        /// Discover possible configurations for this Plugin which the current identity can access
        /// </summary>
        /// <returns>List of named PluginConfigurations which can be used with this Plugin</returns>
        public abstract Task<List<DiscoveredServiceConfiguration>> Discover();

        /// <summary>
        /// Discover possible configurations for this Plugin which the current identity can access
        /// </summary>
        /// <returns>List of named PluginConfigurations which can be used with this Plugin</returns>
        public virtual Task<List<DiscoveredServiceConfiguration>> Discover(List<DiscoveredServiceConfiguration> existingDiscoveries, bool includeSecureElements = false) =>
            Task.FromResult(new List<DiscoveredServiceConfiguration>());
    }
}
