using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json.Serialization;

namespace SecOpsSteward.Plugins.Configurable
{
    /// <summary>
    ///     A parameter which can be used for input to a plugin
    /// </summary>
    public class ConfigurableObjectParameter
    {
        /// <summary>
        ///     Parameter name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     CLR type of parameter
        /// </summary>
        public string ExpectedType { get; set; }

        /// <summary>
        ///     Value of parameter
        /// </summary>
        [JsonConverter(typeof(ObjectDeserializer))]
        public object Value { get; set; }

        /// <summary>
        ///     Display name for parameter
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        ///     Description of the parameter's use
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        ///     If the parameter is used to define the authorization scope for a resource
        /// </summary>
        public bool DefinesAuthorizationScope { get; set; }

        /// <summary>
        ///     If the parameter is required
        /// </summary>
        public bool Required { get; set; }

        /// <summary>
        ///     Create a single BlockParameter from
        /// </summary>
        /// <typeparam name="TConfiguration">Plugin configuration type</typeparam>
        /// <param name="obj">Instance of a Plugin configuration</param>
        /// <param name="propertyInfo">Property underlying the BlockParameter</param>
        /// <returns>Block Parameter based on property</returns>
        public static ConfigurableObjectParameter CreateFromObject<TConfiguration>(TConfiguration obj,
            PropertyInfo propertyInfo)
        {
            var displayName = propertyInfo.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName;
            if (string.IsNullOrEmpty(displayName)) displayName = propertyInfo.Name;

            return new ConfigurableObjectParameter
            {
                Name = propertyInfo.Name,
                DisplayName = displayName,
                Description = propertyInfo.GetCustomAttribute<DescriptionAttribute>()?.Description,
                ExpectedType = propertyInfo.PropertyType.FullName,
                Value = propertyInfo.GetValue(obj),
                DefinesAuthorizationScope =
                    propertyInfo.GetCustomAttribute<IdentifiesTargetGrantScopeAttribute>() != null,
                Required = propertyInfo.GetCustomAttribute<RequiredAttribute>() != null
            };
        }

        public ConfigurableObjectParameter Clone()
        {
            return new()
            {
                Name = Name,
                DisplayName = DisplayName,
                Description = Description,
                ExpectedType = ExpectedType,
                Value = Value,
                DefinesAuthorizationScope = DefinesAuthorizationScope,
                Required = Required
            };
        }

        public override string ToString()
        {
            return $"{DisplayName} ({Name} : {ExpectedType})"
                   + (Value != null ? $" Default: '{Value}'" : "");
        }
    }
}