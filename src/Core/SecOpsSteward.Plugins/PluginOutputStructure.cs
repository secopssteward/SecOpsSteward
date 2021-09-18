using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SecOpsSteward.Plugins
{
    /// <summary>
    ///     Output from a plugin execution
    /// </summary>
    public class PluginOutputStructure
    {
        /// <summary>
        ///     Create an output structure based on a result code
        /// </summary>
        /// <param name="resultCode">Result code</param>
        public PluginOutputStructure(string resultCode)
        {
            ResultCode = resultCode;
        }

        /// <summary>
        ///     Plugin result code
        /// </summary>
        public string ResultCode { get; set; }

        /// <summary>
        ///     Plugin execution exception, if present
        /// </summary>
        public string Exception { get; set; }

        /// <summary>
        ///     Outputs to pass on
        /// </summary>
        public PluginSharedOutputs SharedOutputs { get; set; } = new();

        /// <summary>
        ///     Retrieve a shared output by its name
        /// </summary>
        /// <param name="key">Output name</param>
        /// <returns>Output value</returns>
        public string this[string key]
        {
            get => SharedOutputs[key];
            set => SharedOutputs[key] = value;
        }

        /// <summary>
        ///     Add a secure output to the structure. These will not be passed back to the user at the end of processing.
        /// </summary>
        /// <param name="key">Output name</param>
        /// <param name="value">Output value</param>
        /// <returns>Structure with output included</returns>
        public PluginOutputStructure WithSecureOutput(string key, string value)
        {
            SharedOutputs.SecureOutputs[key] = value;
            return this;
        }

        /// <summary>
        ///     Add an output to the structure. These will be passed back to the user at the end of processing.
        /// </summary>
        /// <param name="key">Output name</param>
        /// <param name="value">Output value</param>
        /// <returns>Structure with output included</returns>
        public PluginOutputStructure WithOutput(string key, string value)
        {
            SharedOutputs.Outputs[key] = value;
            return this;
        }

        /// <summary>
        ///     Collection of outputs shared between step executions
        /// </summary>
        public class PluginSharedOutputs
        {
            /// <summary>
            ///     Secure outputs. These are not passed back to the user at end of processing.
            /// </summary>
            public Dictionary<string, string> SecureOutputs { get; set; } = new();

            /// <summary>
            ///     Public outputs. These are passed back to the user at end of processing.
            /// </summary>
            public Dictionary<string, string> Outputs { get; set; } = new();

            /// <summary>
            ///     Get the value of either a secure or public output.
            ///     When setting a value, it will be created as secure.
            /// </summary>
            /// <param name="key">Output name</param>
            /// <returns>Output value</returns>
            public string this[string key]
            {
                get
                {
                    if (SecureOutputs.ContainsKey(key))
                        return SecureOutputs[key];
                    if (Outputs.ContainsKey(key))
                        return Outputs[key];
                    throw new KeyNotFoundException(key);
                }
                set => SecureOutputs[key] = value;
            }

            /// <summary>
            ///     Check if there are any outputs with a given name
            /// </summary>
            /// <param name="key">Output name</param>
            /// <returns><c>TRUE</c> if the output exists</returns>
            public bool Contains(string key)
            {
                return SecureOutputs.ContainsKey(key) || Outputs.ContainsKey(key);
            }

            /// <summary>
            ///     Retrieve an output by name as a given type
            /// </summary>
            /// <typeparam name="T">Expected output type</typeparam>
            /// <param name="key">Output name</param>
            /// <returns>Output value as type</returns>
            public T As<T>(string key)
            {
                return JsonSerializer.Deserialize<T>(this[key]);
            }

            /// <summary>
            ///     Set an output by name. This will be serialized as JSON.
            /// </summary>
            /// <typeparam name="T">Output value type</typeparam>
            /// <param name="key">Output name</param>
            /// <param name="val">Output value</param>
            public void Set<T>(string key, T val)
            {
                this[key] = JsonSerializer.Serialize(val);
            }

            /// <summary>
            ///     Set a public sharedoutput by name. This will be serialized as JSON.
            /// </summary>
            /// <typeparam name="T">Output value type</typeparam>
            /// <param name="key">Output name</param>
            /// <param name="val">Output value</param>
            public void SetPublicly<T>(string key, T val)
            {
                Outputs[key] = JsonSerializer.Serialize(val);
            }

            /// <summary>
            ///     Retrieve all shared outputs with a given output forced to be public.
            /// </summary>
            /// <param name="key">Output name</param>
            /// <returns>Shared output collection</returns>
            public PluginSharedOutputs AsPublic(string key)
            {
                if (Outputs.ContainsKey(key)) return this;
                if (SecureOutputs.ContainsKey(key))
                {
                    Outputs[key] = SecureOutputs[key];
                    SecureOutputs.Remove(key);
                    return this;
                }

                throw new KeyNotFoundException(key);
            }

            /// <summary>
            ///     Retrieve a collection of outputs which only includes the public outputs.
            /// </summary>
            /// <returns>Collection of public outputs</returns>
            public PluginSharedOutputs AsScrubbedCollection()
            {
                return new PluginSharedOutputs() {Outputs = new Dictionary<string, string>(Outputs)};
            }

            /// <summary>
            ///     Retrieve a clone of this collection of outputs
            /// </summary>
            /// <returns>Clone of this collection of outputs</returns>
            public PluginSharedOutputs AsClone()
            {
                return new PluginSharedOutputs()
                {
                    Outputs = new Dictionary<string, string>(Outputs),
                    SecureOutputs = new Dictionary<string, string>(SecureOutputs)
                };
            }

            /// <summary>
            ///     Purge a pattern of transition outputs based on a string wildcard.
            ///     * : Replace one or more characters
            ///     # : Replace a number
            ///     @ : Replace a letter
            ///     ? : Replace a single character
            /// </summary>
            /// <param name="wildcard">Wildcard string to purge</param>
            /// <param name="secureOnly">Only purge secure outputs</param>
            public PluginSharedOutputs WithPurged(string wildcard, bool secureOnly = true)
            {
                return WithPurged(WildcardToRegex(wildcard), secureOnly);
            }

            /// <summary>
            ///     Purge a pattern of transition outputs based on a regular expression.
            /// </summary>
            /// <param name="regex">Regular expression to filter on</param>
            /// <param name="secureOnly">Only purge secure outputs</param>
            public PluginSharedOutputs WithPurged(Regex regex, bool secureOnly = true)
            {
                SecureOutputs.Keys.Where(k => regex.IsMatch(k)).ToList()
                    .ForEach(k => SecureOutputs.Remove(k));

                if (!secureOnly)
                    Outputs.Keys.Where(k => regex.IsMatch(k)).ToList()
                        .ForEach(k => SecureOutputs.Remove(k));
                return this;
            }

            /// <summary>
            ///     Convert a wildcard expressed to a regular expression
            ///     * : Replace one or more characters
            ///     # : Replace a number
            ///     @ : Replace a letter
            ///     ? : Replace a single character
            /// </summary>
            /// <param name="wildcardExpression">Wildcard expression</param>
            /// <returns>Regular expression equivalent</returns>
            public static Regex WildcardToRegex(string wildcardExpression)
            {
                return new Regex(wildcardExpression
                    .Replace("*", ".+")
                    .Replace("#", "\\d")
                    .Replace("@", "[a-zA-Z]")
                    .Replace("?", "\\w"));
            }
        }
    }
}