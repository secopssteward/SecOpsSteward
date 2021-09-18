using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using SecOpsSteward.Plugins;
using SecOpsSteward.Plugins.Configurable;

namespace SecOpsSteward.Shared.Packaging.Wrappers
{
    /// <summary>
    ///     Wraps Plugin interfaces to handle constructing pointers to methods in the Plugin
    /// </summary>
    public class ContainerPluginWrapper
    {
        public ContainerPluginWrapper(Type pluginType)
        {
            Type genericPluginType;
            if (!pluginType.IsGenericType)
                genericPluginType = pluginType.GetInterfaces().First(i => i.IsGenericType);
            else
                genericPluginType = pluginType;

            PluginType = pluginType;
            ConfigurationType = genericPluginType.GetGenericArguments()[0];
        }

        /// <summary>
        ///     CLR Type which represents the Plugin itself
        /// </summary>
        public Type PluginType { get; }

        /// <summary>
        ///     CLR Type which represents the configuration required for the Plugin
        /// </summary>
        public Type ConfigurationType { get; }

        /// <summary>
        ///     Emit an empty configuration based on the Plugin's ConfigurationType
        /// </summary>
        /// <returns>Blank configuration</returns>
        public IConfigurableObjectConfiguration EmitConfiguration()
        {
            return Activator.CreateInstance(ConfigurationType) as IConfigurableObjectConfiguration;
        }

        /// <summary>
        ///     Emit a populated configuration for a given Plugin based on its serialized JSON
        /// </summary>
        /// <param name="serializedConfiguration">Serialized JSON configuration</param>
        /// <returns>Populated configuration</returns>
        public IConfigurableObjectConfiguration EmitConfiguration(string serializedConfiguration)
        {
            return ChimeraSharedHelpers.GetFromSerializedString(serializedConfiguration, ConfigurationType) as
                IConfigurableObjectConfiguration;
        }

        /// <summary>
        ///     Emits an instance of the Plugin type
        /// </summary>
        /// <returns>Plugin instance</returns>
        public IPlugin Emit(IServiceProvider services)
        {
            return ActivatorUtilities.CreateInstance(services, PluginType) as IPlugin;
        }

        /// <summary>
        ///     Emits an instance of the Plugin type without the service provider (cannot execute)
        /// </summary>
        /// <returns>Plugin instance</returns>
        public IPlugin Emit()
        {
            return Activator.CreateInstance(PluginType) as IPlugin;
        }

        /// <summary>
        ///     Emits an instance of the Plugin type, with its configuration applied
        /// </summary>
        /// <param name="serializedConfiguration">Serialized JSON configuration for Plugin</param>
        /// <returns>Configured Plugin instance</returns>
        public IPlugin Emit(IServiceProvider services, string serializedConfiguration)
        {
            var instance = Emit(services);
            var configProp = PluginType.GetProperty("Configuration");
            if (!string.IsNullOrEmpty(serializedConfiguration))
            {
                var configDeserialized =
                    ChimeraSharedHelpers.GetFromSerializedString(serializedConfiguration, ConfigurationType);
                configProp.SetValue(instance, configDeserialized);
            }
            else
            {
                configProp.SetValue(instance, Activator.CreateInstance(ConfigurationType));
            }

            return instance;
        }

        /// <summary>
        ///     Get the contract associated with configuring a Plugin
        /// </summary>
        /// <returns>List of parameters which can serve as inputs to a Plugin</returns>
        public ConfigurableObjectParameterCollection GetContract()
        {
            var configInstance = Activator.CreateInstance(ConfigurationType) as IConfigurableObjectConfiguration;
            return ConfigurableObjectParameterCollection.CreateFromObject(configInstance);
        }

        /// <summary>
        ///     Get the possible outputs which can happen from running a Plugin
        /// </summary>
        /// <returns>Possible outputs</returns>
        public string[] GetOutputs()
        {
            var attr = PluginType.GetCustomAttribute<PossibleResultCodesAttribute>();
            if (attr == null) return new string[0];
            return attr.Outputs;
        }
    }
}