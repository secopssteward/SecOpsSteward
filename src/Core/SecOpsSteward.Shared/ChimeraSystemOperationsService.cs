using Microsoft.Extensions.Logging;
using SecOpsSteward.Shared.Configuration;
using SecOpsSteward.Shared.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SecOpsSteward.Shared
{
    /// <summary>
    /// A service which handles invocation of batch operations on all Chimera services
    /// </summary>
    public class ChimeraSystemOperationsService
    {
        private readonly ILogger<ChimeraSystemOperationsService> _logger;
        private readonly IEnumerable<IChimeraIntegratedService> _services;
        private readonly IConfigurationProvider _configurationProvider;

        /// <summary>
        /// A service which handles invocation of batch operations on all Chimera services
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="services"></param>
        /// <param name="configurationProvider"></param>
        public ChimeraSystemOperationsService(
            ILogger<ChimeraSystemOperationsService> logger,
            IEnumerable<IChimeraIntegratedService> services,
            IConfigurationProvider configurationProvider)
        {
            _logger = logger;
            _services = services;
            _configurationProvider = configurationProvider;
        }

        /// <summary>
        /// All registered Chimera services which this service interfaces with
        /// </summary>
        public IReadOnlyList<IChimeraIntegratedService> Services => _services.ToList();

        /// <summary>
        /// Retrieve an Agent's configuration and commits it back to the configuration provider once complete
        /// </summary>
        /// <param name="agent">Agent</param>
        /// <param name="action">Action to perform on configuration</param>
        /// <param name="isReadOnly">If <c>TRUE</c>, no commit action will be taken</param>
        /// <returns></returns>
        public async Task WithConfiguration(ChimeraAgentIdentifier agent, Action<AgentConfiguration> action, bool isReadOnly = false)
        {
            var config = await _configurationProvider.GetConfiguration(agent);
            action(config);
            if (!isReadOnly)
                await _configurationProvider.UpdateConfiguration(agent, config);
        }

        /// <summary>
        /// List all registered agents by their configuration
        /// </summary>
        /// <returns>All configurations</returns>
        public Task<List<AgentConfiguration>> ListRegisteredAgents() => _configurationProvider.ListConfigurations();

        /// <summary>
        /// Create a new Chimera Agent
        /// </summary>
        /// <param name="agentId">New agent ID</param>
        /// <returns></returns>
        public Task CreateAgent(ChimeraAgentIdentifier agentId) =>
            RunOnAllServices<IHasAgentCreationActions>(s => s.OnAgentCreated(agentId));

        /// <summary>
        /// Destroy an existing Chimera Agent
        /// </summary>
        /// <param name="agentId">Agent ID to destroy</param>
        /// <returns></returns>
        public Task DestroyAgent(ChimeraAgentIdentifier agentId) =>
            RunOnAllServices<IHasAgentCreationActions>(s => s.OnAgentRemoved(agentId), true);

        /// <summary>
        /// Create/enroll a new User in the Chimera system
        /// </summary>
        /// <param name="userId">User ID to enroll</param>
        /// <returns></returns>
        public Task CreateUser(ChimeraUserIdentifier userId) =>
            RunOnAllServices<IHasUserEnrollmentActions>(s => s.OnUserEnrolled(userId));

        /// <summary>
        /// Destroy/unenroll a User from the Chimera system
        /// </summary>
        /// <param name="userId">User ID to remove</param>
        /// <returns></returns>
        public Task DestroyUser(ChimeraUserIdentifier userId) =>
            RunOnAllServices<IHasUserEnrollmentActions>(s => s.OnUserRemoved(userId), true);

        private async Task RunOnAllServices<TService>(Func<TService, Task> cmd, bool runBackwards = false)
            where TService : IChimeraIntegratedService
        {
            var services = GetServices<TService>();
            if (runBackwards) services = services.Reverse();

            _logger.LogTrace("-- System EXECUTE : {tservice} --", typeof(TService).Name);
            foreach (var group in services)
            {
                _logger.LogTrace("--> Group priority {priority} -- {count} services", group.Key, group.Count());
                await Task.WhenAll(group.Select(async g =>
                {
                    try
                    {
                        _logger.LogTrace("START running service {service}", g.GetType().Name);
                        await cmd(g);
                        _logger.LogTrace("END running service {service}", g.GetType().Name);
                    }
                    catch (Exception ex)
                    { _logger.LogError(ex, $"Error executing command for {g.GetType().Name}"); }
                }));
            }
        }

        private IEnumerable<IGrouping<int, TService>> GetServices<TService>()
            where TService : IChimeraIntegratedService => _services
                .Where(s => s is TService)
                .Cast<TService>()
                .GroupBy(s => s.ServicePriority)
                .OrderBy(k => k.Key);
    }
}
