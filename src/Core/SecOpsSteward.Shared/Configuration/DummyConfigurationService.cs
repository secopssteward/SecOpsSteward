using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SecOpsSteward.Shared.Configuration
{
    public class DummyConfigurationService : IConfigurationProvider
    {
        private readonly ILogger<DummyConfigurationService> _logger;
        public static List<AgentConfiguration> Configurations { get; set; } = new List<AgentConfiguration>();

        public DummyConfigurationService(ILogger<DummyConfigurationService> logger) => _logger = logger;

        public async Task<AgentConfiguration> GetConfiguration(ChimeraAgentIdentifier agent)
        {
            await Task.Yield();
            _logger.LogTrace($"Config being retrieved for {agent}");
            return Configurations.FirstOrDefault(c => c.AgentId == agent);
        }

        public async Task<List<AgentConfiguration>> ListConfigurations()
        {
            await Task.Yield();
            _logger.LogTrace($"Configs being listed");
            return Configurations;
        }

        public async Task UpdateConfiguration(ChimeraAgentIdentifier agent, AgentConfiguration configuration)
        {
            await Task.Yield();
            _logger.LogTrace($"Config being updated for {agent}");
            Configurations.RemoveAll(c => c.AgentId == agent);
            configuration.AgentId = agent;
            Configurations.Add(configuration);
        }
    }
}
