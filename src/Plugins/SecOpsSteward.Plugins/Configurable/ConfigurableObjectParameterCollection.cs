using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SecOpsSteward.Plugins.Configurable
{
    /// <summary>
    ///     A collection of parameters which can be used for input to a plugin
    /// </summary>
    public class ConfigurableObjectParameterCollection
    {
        /// <summary>
        ///     A collection of parameters which can be used for input to a plugin
        /// </summary>
        public ConfigurableObjectParameterCollection()
        {
        }

        /// <summary>
        ///     A collection of parameters which can be used for input to a plugin
        /// </summary>
        /// <param name="parameters">Existing parameters to add</param>
        public ConfigurableObjectParameterCollection(IEnumerable<ConfigurableObjectParameter> parameters)
        {
            Parameters = parameters.ToList();
        }

        /// <summary>
        ///     A collection of parameters which can be used for input to a plugin
        /// </summary>
        /// <param name="dictionary">Existing parameters to add from a Dictionary of values</param>
        public ConfigurableObjectParameterCollection(IDictionary<string, object> dictionary) : this(
            dictionary.Select(d => new ConfigurableObjectParameter {Name = d.Key, Value = d.Value}))
        {
        }

        /// <summary>
        ///     All stored parameters
        /// </summary>
        public List<ConfigurableObjectParameter> Parameters { get; set; } = new();

        /// <summary>
        ///     Accessor for a specific parameter by name
        /// </summary>
        /// <param name="name">Parameter name</param>
        /// <returns>Parameter value</returns>
        [JsonIgnore]
        public object this[string name]
        {
            get => Parameters.FirstOrDefault(p => p.Name == name)?.Value;
            set => Parameters.FirstOrDefault(p => p.Name == name).Value = value;
        }

        /// <summary>
        ///     If the values are populated which define the target's grant scope
        /// </summary>
        public bool GrantScopeValuesPopulated =>
            !Parameters.Where(p => p.DefinesAuthorizationScope).Any(p => IsValueEmpty(p.Value));

        /// <summary>
        ///     If all required values are populated
        /// </summary>
        public bool RequiredValuesPopulated => !Parameters.Where(p => p.Required).Any(p => IsValueEmpty(p.Value));

        /// <summary>
        ///     Apply this configuration to a templated string (each value is prepended with "Configuration.")
        /// </summary>
        /// <param name="template">Template string</param>
        /// <returns>Populated string</returns>
        public string ApplyToStringTemplate(string template)
        {
            var emulatedStructure = new PluginOutputStructure("");
            foreach (var param in Parameters)
                emulatedStructure.SharedOutputs.SecureOutputs.Add("Configuration." + param.Name,
                    param.Value == null ? "" : param.Value.ToString());
            return TemplatedStrings.PopulateInputsInTemplateString(template, emulatedStructure);
        }

        /// <summary>
        ///     Representation of all Parameters and values as a Dictionary
        /// </summary>
        public IDictionary<string, object> AsDictionary()
        {
            return Parameters.ToDictionary(k => k.Name, v => v.Value);
        }

        /// <summary>
        ///     Get the hashcode for properties which are used to identify the authorization grant scope of a resource.
        ///     This is used to uniquely identify resources' authorization scopes for the purposes of access management.
        /// </summary>
        /// <param name="config">Configuration to generate hashcode for</param>
        /// <returns>Hashcode of authorization scope</returns>
        public int GetConfigurationGrantScopeHashCode()
        {
            var values = Parameters.Where(p => p.DefinesAuthorizationScope).OrderBy(p => p.Name).Select(p => p.Value);
            var hashCode = -1;
            foreach (var v in values)
            {
                if (v == null) continue;
                if (hashCode == -1)
                    hashCode = v.GetHashCode();
                else
                    hashCode ^= v.GetHashCode();
            }

            return hashCode;
        }

        /// <summary>
        ///     Representation of all Parameters and values as a serialized string
        /// </summary>
        public string AsSerializedString()
        {
            return JsonSerializer.Serialize(AsDictionary());
        }

        /// <summary>
        ///     Apply values from a serialized string
        /// </summary>
        /// <param name="str">Serialized string values</param>
        /// <returns>Populated parameter collection</returns>
        public ConfigurableObjectParameterCollection WithSerializedStringValues(string str)
        {
            var values = JsonSerializer.Deserialize<Dictionary<string, object>>(str);
            foreach (var value in values)
                if (Parameters.Any(p => p.Name == value.Key))
                    Parameters.First(p => p.Name == value.Key).Value = value;
            return this;
        }

        /// <summary>
        ///     Create a PluginParameterCollection based on a configuration class for a Package plugin
        /// </summary>
        /// <typeparam name="TConfiguration">Plugin configuration type</typeparam>
        /// <param name="obj">Instance of a Plugin configuration</param>
        /// <returns></returns>
        public static ConfigurableObjectParameterCollection CreateFromObject<TConfiguration>(TConfiguration obj)
        {
            return new(
                obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
                    .Where(p => p.CanWrite)
                    .Select(p => ConfigurableObjectParameter.CreateFromObject(obj, p)));
        }

        public ConfigurableObjectParameterCollection Clone()
        {
            return new(Parameters.Select(p => p.Clone()));
        }

        private bool IsValueEmpty(object value)
        {
            if (value == null || value == default) return true;
            if (value is string) return string.IsNullOrEmpty(value as string);
            return false;
        }
    }
}