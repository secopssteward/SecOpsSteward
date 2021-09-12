using System;
using System.Collections.Generic;

namespace SecOpsSteward.Shared
{
    /// <summary>
    /// An object which holds the configuration options for the Chimera service
    /// </summary>
    public class ChimeraServiceConfigurator
    {
        /// <summary>
        /// Options which apply to parts of the service
        /// </summary>
        public Dictionary<string, string> Options { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Functions which output dynamic results for configuration values
        /// </summary>
        public Dictionary<string, Func<string>> Derivations { get; set; } = new Dictionary<string, Func<string>>();

        /// <summary>
        /// Access a configuration item by its key
        /// </summary>
        /// <param name="configItem">Item key to access</param>
        /// <returns></returns>
        public string this[string configItem]
        {
            get
            {
                if (Derivations.ContainsKey(configItem)) return Derivations[configItem]();
                return Options[configItem];
            }
            set
            {
                if (Derivations.ContainsKey(configItem)) return; // no change
                Options[configItem] = value;
            }
        }

        public ChimeraServiceConfigurator() { }
        public ChimeraServiceConfigurator(Dictionary<string, string> values) => Options = values;
    }
}
