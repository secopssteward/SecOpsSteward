using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using SecOpsSteward.Plugins.Configurable;
using SecOpsSteward.Plugins.Discovery;
using SecOpsSteward.Plugins.WorkflowTemplates;

namespace SecOpsSteward.Plugins
{
    [Flags]
    public enum ManagedServiceRole
    {
        None = 0,

        /// <summary>
        ///     Has one or more elements which are consumed elsewhere.
        ///     An example of this is a service accessed using a password.
        /// </summary>
        Producer = 1,

        /// <summary>
        ///     Consumes one or more elements from a Producer.
        ///     An example of this is a web application which connects to a service with a password.
        /// </summary>
        Consumer = 2,

        /// <summary>
        ///     Is both a Producer and Consumer.
        ///     An example of this is a password storage vault; it can be consumed by other services, but also consumes passwords.
        /// </summary>
        Hybrid = 3
    }

    /// <summary>
    ///     Package for all plugins associated with a managed service
    /// </summary>
    public interface IManagedServicePackage : IConfigurableObject, IHasDescriptiveMetadata
    {
        /// <summary>
        ///     Role this managed service plays in systems
        /// </summary>
        ManagedServiceRole Role { get; }

        /// <summary>
        ///     Templates which work with this managed service
        /// </summary>
        List<WorkflowTemplateDefinition> Templates { get; }

        /// <summary>
        ///     Discover possible configurations for this Plugin which the current identity can access
        /// </summary>
        /// <returns>List of named PluginConfigurations which can be used with this Plugin</returns>
        Task<List<DiscoveredServiceConfiguration>> Discover();

        /// <summary>
        ///     Discover possible configurations for this Plugin which the current identity can access
        /// </summary>
        /// <returns>List of named PluginConfigurations which can be used with this Plugin</returns>
        Task<List<DiscoveredServiceConfiguration>> Discover(List<DiscoveredServiceConfiguration> existingDiscoveries,
            bool includeSecureElements = false);
    }

    /// <summary>
    ///     Package for all plugins associated with a managed service which takes a configuration object
    /// </summary>
    /// <typeparam name="TConfiguration">Plugin configuration type</typeparam>
    public interface IManagedServicePackage<TConfiguration> : IManagedServicePackage
        where TConfiguration : IConfigurableObjectConfiguration
    {
        /// <summary>
        ///     Managed service configuration
        /// </summary>
        TConfiguration Configuration { get; set; }
    }

    public static class IManagedServicePackageExtensions
    {
        public static IPlugin CreatePlugin(this IManagedServicePackage package, IServiceProvider services,
            Guid pluginId, IConfigurableObjectConfiguration config = null)
        {
            var asm = package.GetType().Assembly;
            var plugins = asm.GetTypes().Where(t => t.IsSubclassOf(typeof(IPlugin)));

            var target = plugins.FirstOrDefault(p => p.GenerateId() == pluginId);
            if (target == null) return null;

            var baseConfiguration = package.GetConfigurationObject();

            var instance = ActivatorUtilities.CreateInstance(services, target) as IPlugin;
            if (instance.GetConfigurationObject() == null)
                instance.WithConfiguration(instance.GetBlankConfiguration());

            if (config != null)
                instance.WithConfiguration(
                    instance.GetConfigurationObject()
                        .Merge(baseConfiguration)
                        .Merge(config));
            else
                instance.WithConfiguration(
                    instance.GetConfigurationObject()
                        .Merge(baseConfiguration));
            return instance;
        }

        public static IPlugin CreatePlugin(this IManagedServicePackage package, IPlugin plugin,
            IConfigurableObjectConfiguration config = null)
        {
            var baseConfiguration = package.GetConfigurationObject();

            if (plugin.GetConfigurationObject() == null)
                plugin.WithConfiguration(plugin.GetBlankConfiguration());

            if (config != null)
                plugin.GetConfigurationObject()
                    .Merge(baseConfiguration)
                    .Merge(config);
            else
                plugin.GetConfigurationObject()
                    .Merge(baseConfiguration);

            return plugin;
        }
    }
}