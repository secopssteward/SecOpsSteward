using System;
using System.Reflection;

namespace SecOpsSteward.Plugins
{
    public interface IHasDescriptiveMetadata { }

    public class ElementDescriptionAttribute : Attribute
    {
        /// <summary>
        /// Display name for element
        /// </summary>
        public string PluginName { get; }

        /// <summary>
        /// Description of what element does
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Element author
        /// </summary>
        public string Author { get; }

        /// <summary>
        /// Version
        /// </summary>
        public string Version { get; }

        /// <summary>
        /// Version object (parsed)
        /// </summary>
        public Version VersionObj => new Version(Version);

        public ElementDescriptionAttribute(
            string pluginName,
            string author,
            string description,
            string version)
        {
            PluginName = pluginName;
            Author = author;
            Description = description;
            Version = version;
        }

        public ElementDescriptionAttribute(
            string pluginName,
            string description)
        {
            PluginName = pluginName;
            Description = description;
        }
    }

    public static class IHasDescriptiveMetadataExtensions
    {
        /// <summary>
        /// Get the name of a Plugin from its metadata
        /// </summary>
        /// <param name="element">Plugin to inspect</param>
        /// <returns>Plugin name</returns>
        public static string GetDescriptiveName(this IHasDescriptiveMetadata element) => GetMetaAttribute(element)?.PluginName;

        /// <summary>
        /// Get the author of a Plugin from its metadata
        /// </summary>
        /// <param name="element">Plugin to inspect</param>
        /// <returns>Plugin author</returns>
        public static string GetDescriptiveAuthor(this IHasDescriptiveMetadata element) => GetMetaAttribute(element)?.Author;

        /// <summary>
        /// Get the version of a Plugin from its metadata
        /// </summary>
        /// <param name="element">Plugin to inspect</param>
        /// <returns>Plugin version</returns>
        public static Version GetDescriptiveVersion(this IHasDescriptiveMetadata element) => GetMetaAttribute(element)?.VersionObj;

        /// <summary>
        /// Get the description of a Plugin from its metadata
        /// </summary>
        /// <param name="element">Plugin to inspect</param>
        /// <returns>Plugin description</returns>
        public static string GetDescriptiveDescription(this IHasDescriptiveMetadata element) => GetMetaAttribute(element)?.Description;

        private static ElementDescriptionAttribute GetMetaAttribute(IHasDescriptiveMetadata plugin) =>
            plugin.GetType().GetCustomAttribute<ElementDescriptionAttribute>();
    }
}
