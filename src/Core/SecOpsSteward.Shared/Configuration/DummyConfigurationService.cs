using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SecOpsSteward.Shared.Configuration
{
    public class DummyConfigurationService : IConfigurationProvider
    {
        private readonly ILogger<DummyConfigurationService> _logger;

        public DummyConfigurationService(ILogger<DummyConfigurationService> logger)
        {
            _logger = logger;
        }

        public static List<AgentConfiguration> Configurations { get; set; } = new();

        public async Task<AgentConfiguration> GetConfiguration(ChimeraAgentIdentifier agent)
        {
            await Task.Yield();
            _logger.LogTrace($"Config being retrieved for {agent}");
            if (!Configurations.Any(c => c.AgentId == agent))
                return new AgentConfiguration {AgentId = agent};
            return Configurations.FirstOrDefault(c => c.AgentId == agent);
        }

        public async Task<List<AgentConfiguration>> ListConfigurations()
        {
            await Task.Yield();
            _logger.LogTrace("Configs being listed");
            return Configurations;
        }

        public async Task UpdateConfiguration(ChimeraAgentIdentifier agent, AgentConfiguration configuration)
        {
            await Task.Yield();
            _logger.LogTrace($"Config being updated for {agent}");
            if (Configurations.Any(c => c.AgentId == agent))
                Configurations.RemoveAll(c => c.AgentId == agent);
            configuration.AgentId = agent;
            Configurations.Add(configuration);
        }
    }
}