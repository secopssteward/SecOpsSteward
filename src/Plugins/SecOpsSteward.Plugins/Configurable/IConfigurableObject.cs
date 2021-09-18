using System;
using System.Collections.Generic;
using System.Linq;

namespace SecOpsSteward.Plugins.Configurable
{
    public interface IConfigurableObjectConfiguration
    {
    }

    public interface IConfigurableObject
    {
    } // implies TConfiguration Configuration property

    public static class ConfigurableObjectConfigurationExtensions
    {
        /// <summary>
        ///     Apply this configuration to a templated string (each value is prepended with "Configuration.")
        /// </summary>
        /// <param name="template">Template string</param>
        /// <returns>Populated string</returns>
        public static string PopulateStringTemplate(this IConfigurableObjectConfiguration config, string template)
        {
            var emulatedStructure = new PluginOutputStructure("");
            foreach (var param in config.AsDictionaryProperties())
                emulatedStructure.SharedOutputs.SecureOutputs.Add("Configuration." + param.Key,
                    param.Value == null ? "" : param.Value.ToString());
            return TemplatedStrings.PopulateInputsInTemplateString(template, emulatedStructure);
        }

        /// <summary>
        ///     Merge a configuration with a single item
        /// </summary>
        /// <param name="configA">Configuration to merge into</param>
        /// <param name="name">Config item name</param>
        /// <param name="value">Config item value</param>
        /// <returns>Merged configuration</returns>
        public static IConfigurableObjectConfiguration Merge(this IConfigurableObjectConfiguration configA, string name,
            object value)
        {
            return Merge(configA, new Dictionary<string, object> {{name, value}});
        }

        /// <summary>
        ///     Merge a configuration with another; duplicates will be resolved from the "B" configuration
        /// </summary>
        /// <param name="configA">Configuration to merge into</param>
        /// <param name="configB">Configuration being merged</param>
        /// <returns>Merged configuration</returns>
        public static IConfigurableObjectConfiguration Merge(this IConfigurableObjectConfiguration configA,
            IConfigurableObjectConfiguration configB)
        {
            return Merge(configA, configB.AsDictionaryProperties());
        }

        /// <summary>
        ///     Merge a configuration with another; duplicates will be resolved from the "B" configuration
        /// </summary>
        /// <param name="configA">Configuration to merge into</param>
        /// <param name="configBDictionary">Configuration being merged as a Dictionary of values</param>
        /// <returns>Merged configuration</returns>
        public static IConfigurableObjectConfiguration Merge(this IConfigurableObjectConfiguration configA,
            Dictionary<string, object> configBDictionary)
        {
            var baseConfigA = configA.AsDictionaryProperties();
            foreach (var item in configBDictionary)
            {
                if (item.Value == null) continue;
                baseConfigA[item.Key] = item.Value;
            }

            return baseConfigA.AsObject(configA.GetType()) as IConfigurableObjectConfiguration;
        }

        /// <summary>
        ///     Clone this configuration
        /// </summary>
        /// <param name="config">Configuration to clone</param>
        /// <returns>Object clone of configuration</returns>
        public static IConfigurableObjectConfiguration Clone(this IConfigurableObjectConfiguration config)
        {
            var dict = config.AsDictionaryProperties();
            return dict.AsObject(config.GetType()) as IConfigurableObjectConfiguration;
        }
    }

    public static class IConfigurableObjectExtensions
    {
        /// <summary>
        ///     Get a blank instance of a Configuration type
        /// </summary>
        /// <param name="basis">Configurable object to get type from</param>
        /// <returns>Blank Configuration instance</returns>
        public static IConfigurableObjectConfiguration GetBlankConfiguration(this IConfigurableObject basis)
        {
            return Activator.CreateInstance(basis.GetConfigurationType()) as IConfigurableObjectConfiguration;
        }

        /// <summary>
        ///     Apply a Configuration to a configurable object
        /// </summary>
        /// <typeparam name="TConfigurable">Configurable object type</typeparam>
        /// <typeparam name="TConfiguration">Configuration type</typeparam>
        /// <param name="target">Object to configure</param>
        /// <param name="config">Configuration</param>
        /// <returns>Configured object</returns>
        public static TConfigurable WithConfiguration<TConfigurable, TConfiguration>(this TConfigurable target,
            TConfiguration config)
            where TConfigurable : IConfigurableObject
            where TConfiguration : IConfigurableObjectConfiguration
        {
            var configProperty = target.GetType().GetProperty("Configuration");
            configProperty.SetValue(target, config);
            return target;
        }

        /// <summary>
        ///     Apply a Configuration to a configurable object
        /// </summary>
        /// <typeparam name="TConfigurable">Configurable object type</typeparam>
        /// <param name="target">Object to configure</param>
        /// <param name="dictionaryConfig">Configuration as a Dictionary of values</param>
        /// <returns>Configured object</returns>
        public static TConfigurable WithConfiguration<TConfigurable>(this TConfigurable target,
            IDictionary<string, object> dictionaryConfig)
            where TConfigurable : IConfigurableObject
        {
            return target.WithConfiguration(
                dictionaryConfig.AsObject(target.GetConfigurationType()) as IConfigurableObjectConfiguration);
        }

        /// <summary>
        ///     Retrieve the Configuration from an object as a Dictionary of values
        /// </summary>
        /// <param name="basis">Object to extract configuration from</param>
        /// <returns>Dictionary of Configuration values</returns>
        public static Dictionary<string, object> GetConfigurationAsDictionary(this IConfigurableObject basis)
        {
            return basis.GetConfigurationObject().AsDictionaryProperties();
        }

        /// <summary>
        ///     Retrieve the Configuration from an object as its expected Configuration type
        /// </summary>
        /// <param name="basis">Object to extract configuration from</param>
        /// <returns>Configuration object</returns>
        public static IConfigurableObjectConfiguration GetConfigurationObject(this IConfigurableObject basis)
        {
            var configProperty = basis.GetType().GetProperty("Configuration");
            return configProperty.GetValue(basis) as IConfigurableObjectConfiguration;
        }

        /// <summary>
        ///     Retrieve the Configuration type of a configurable object
        /// </summary>
        /// <param name="basis">Configurable object to inspect</param>
        /// <returns>Configuration type of object</returns>
        public static Type GetConfigurationType(this IConfigurableObject basis)
        {
            Type genericPluginType;
            if (!basis.GetType().IsGenericType)
                genericPluginType = basis.GetType().GetInterfaces().First(i => i.IsGenericType);
            else
                genericPluginType = basis.GetType();
            return genericPluginType.GetGenericArguments()[0];
        }

        /// <summary>
        ///     Retrieve the description of the Configuration of an object
        /// </summary>
        /// <param name="basis">Object to describe the Configuration of</param>
        /// <returns>Configuration parameters</returns>
        public static ConfigurableObjectParameterCollection GetConfigurationDescription(this IConfigurableObject basis)
        {
            return ConfigurableObjectParameterCollection.CreateFromObject(
                Activator.CreateInstance(basis.GetConfigurationType()));
        }
    }
}