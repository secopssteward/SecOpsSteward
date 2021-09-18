using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using SecOpsSteward.Plugins;
using SecOpsSteward.Plugins.Configurable;

namespace SecOpsSteward.Shared.Packaging.Wrappers
{
    /// <summary>
    ///     Wraps Service interfaces to handle constructing pointers to methods in the Service
    /// </summary>
    public class ContainerServiceWrapper
    {
        public ContainerServiceWrapper(Type serviceType)
        {
            Type genericServiceType;
            if (!serviceType.IsGenericType)
                genericServiceType = serviceType.GetInterfaces().First(i => i.IsGenericType);
            else
                genericServiceType = serviceType;

            ServiceType = serviceType;
            ConfigurationType = genericServiceType.GetGenericArguments()[0];
        }

        /// <summary>
        ///     CLR Type which represents the Service itself
        /// </summary>
        public Type ServiceType { get; }

        /// <summary>
        ///     CLR Type which represents the configuration required for the Service
        /// </summary>
        public Type ConfigurationType { get; }

        /// <summary>
        ///     Emit an empty configuration based on the Service's ConfigurationType
        /// </summary>
        /// <returns>Blank configuration</returns>
        public IConfigurableObjectConfiguration EmitConfiguration()
        {
            return Activator.CreateInstance(ConfigurationType) as IConfigurableObjectConfiguration;
        }

        /// <summary>
        ///     Emit a populated configuration for a given Service based on its serialized JSON
        /// </summary>
        /// <param name="serializedConfiguration">Serialized JSON configuration</param>
        /// <returns>Populated configuration</returns>
        public IConfigurableObjectConfiguration EmitConfiguration(string serializedConfiguration)
        {
            return ChimeraSharedHelpers.GetFromSerializedString(serializedConfiguration, ConfigurationType) as
                IConfigurableObjectConfiguration;
        }

        /// <summary>
        ///     Emits an instance of the Service type
        /// </summary>
        /// <returns>Service instance</returns>
        public IManagedServicePackage Emit(IServiceProvider services)
        {
            return ActivatorUtilities.CreateInstance(services, ServiceType) as IManagedServicePackage;
        }

        /// <summary>
        ///     Emits an instance of the Service type without the service provider (cannot discover)
        /// </summary>
        /// <returns>Service instance</returns>
        public IManagedServicePackage Emit()
        {
            return Activator.CreateInstance(ServiceType) as IManagedServicePackage;
        }

        /// <summary>
        ///     Emits an instance of the Service type, with its configuration applied
        /// </summary>
        /// <param name="serializedConfiguration">Serialized JSON configuration for Service</param>
        /// <returns>Configured Service instance</returns>
        public IManagedServicePackage Emit(IServiceProvider services, string serializedConfiguration)
        {
            var instance = Emit(services);
            var configProp = ServiceType.GetProperty("Configuration");
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
        ///     Get the contract associated with configuring a Service
        /// </summary>
        /// <returns>List of parameters which can serve as inputs to a Service</returns>
        public ConfigurableObjectParameterCollection GetContract()
        {
            var configInstance = Activator.CreateInstance(ConfigurationType) as IConfigurableObjectConfiguration;
            return ConfigurableObjectParameterCollection.CreateFromObject(configInstance);
        }
    }
}