using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SecOpsSteward.Plugins.Configurable;
using static SecOpsSteward.Plugins.PluginOutputStructure;

namespace SecOpsSteward.Plugins
{
    public static class IPluginExtensions
    {
        /// <summary>
        ///     Verify if the input options of a Plugin match a given set of output options
        /// </summary>
        /// <param name="plugin">Plugin to verify</param>
        /// <param name="outputs">Output options to compare</param>
        /// <returns><c>TRUE</c> if the options are compatible, otherwise <c>FALSE</c></returns>
        public static bool AreSharedOptionsCompatible(this IPlugin plugin, List<string> outputs)
        {
            var configuration = plugin.GetConfigurationObject();
            if (!plugin.GetRequiredSharedInputs().Any()) return true;
            return plugin.GetRequiredSharedInputs().All(
                input => outputs.Any(o =>
                {
                    var outputStr = configuration.PopulateStringTemplate(o);
                    return PluginSharedOutputs.WildcardToRegex(outputStr).IsMatch(input);
                }));
        }

        /// <summary>
        ///     Get a collection of possible result codes from a Plugin
        /// </summary>
        /// <param name="plugin">Plugin to inspect</param>
        /// <returns>Possible result codes</returns>
        public static List<string> GetPossibleResultCodes(this IPlugin plugin)
        {
            var attr = plugin.GetType().GetCustomAttribute<PossibleResultCodesAttribute>();
            if (attr == null) return new List<string>();
            return attr.Outputs.ToList();
        }

        /// <summary>
        ///     Get a collection of required input keys for a Plugin
        /// </summary>
        /// <param name="plugin">Plugin to inspect</param>
        /// <returns>Required input keys</returns>
        public static List<string> GetRequiredSharedInputs(this IPlugin plugin)
        {
            var attr = plugin.GetType().GetCustomAttribute<RequiredSharedInputsAttribute>();
            if (attr == null) return new List<string>();
            return attr.RequiredInputs.ToList();
        }

        /// <summary>
        ///     Get a collection of generated output keys for a Plugin
        /// </summary>
        /// <param name="plugin">Plugin to inspect</param>
        /// <returns>Generated output keys</returns>
        public static List<string> GetGeneratedSharedOutputs(this IPlugin plugin)
        {
            var attr = plugin.GetType().GetCustomAttribute<GeneratedSharedOutputsAttribute>();
            if (attr == null) return new List<string>();
            return attr.GeneratedOutputs.ToList();
        }
    }
}