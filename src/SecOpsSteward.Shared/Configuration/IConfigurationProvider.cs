using System.Collections.Generic;
using System.Threading.Tasks;

namespace SecOpsSteward.Shared.Configuration
{
    /// <summary>
    ///     Handles the configuration and access management for Agents
    /// </summary>
    public interface IConfigurationProvider
    {
        /// <summary>
        ///     List all Agent configurations in storage
        /// </summary>
        /// <returns>List of Agent configurations</returns>
        Task<List<AgentConfiguration>> ListConfigurations();

        /// <summary>
        ///     Retrieve an Agent's configuration
        /// </summary>
        /// <param name="agent">Agent</param>
        /// <returns>Configuration object</returns>
        Task<AgentConfiguration> GetConfiguration(ChimeraAgentIdentifier agent);

        /// <summary>
        ///     Update an Agent's configuration
        /// </summary>
        /// <param name="agent">Agent</param>
        /// <param name="configuration">New configuration</param>
        /// <returns></returns>
        Task UpdateConfiguration(ChimeraAgentIdentifier agent, AgentConfiguration configuration);
    }
}