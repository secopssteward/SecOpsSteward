using Azure;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;
using SecOpsSteward.Plugins.Azure;
using SecOpsSteward.Shared;
using SecOpsSteward.Shared.Configuration;
using SecOpsSteward.Shared.Roles;
using SecOpsSteward.Shared.Services;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace SecOpsSteward.Integrations.Azure.Configuration
{
    public class AzureKeyVaultConfigurationService : AzureKeyVaultIntegration, IConfigurationProvider, IHasAgentCreationActions
    {
        public int ServicePriority => 20;

        public AzureKeyVaultConfigurationService(
            ILogger<AzureKeyVaultConfigurationService> logger,
            ChimeraServiceConfigurator configurator,
            IRoleAssignmentService roleAssignment,
            AzureCurrentCredentialFactory platformFactory) : base(logger, configurator, roleAssignment, platformFactory) { }

        public async Task<AgentConfiguration> GetConfiguration(ChimeraAgentIdentifier agent)
        {
            var secret = (await GetSecretClient().GetSecretAsync(GetConfigSecretName(agent))).Value;
            return ChimeraSharedHelpers.GetFromSerializedString<AgentConfiguration>(secret.Value);
        }

        public async Task<List<AgentConfiguration>> ListConfigurations()
        {
            var secrets = GetSecretClient().GetPropertiesOfSecrets().Where(s => s.Name.EndsWith("-config"));
            return (await Task.WhenAll(secrets.Select(async s =>
            {
                var configStr = await GetSecretClient().GetSecretAsync(s.Name);
                return ChimeraSharedHelpers.GetFromSerializedString<AgentConfiguration>(configStr.Value.Value);
            }))).ToList();
        }

        public async Task UpdateConfiguration(ChimeraAgentIdentifier agent, AgentConfiguration configuration)
        {
            var value = ChimeraSharedHelpers.SerializeToString(configuration);
            await GetSecretClient().SetSecretAsync(GetConfigSecretName(agent), value);
        }

        // ---

        public async Task OnAgentCreated(ChimeraAgentIdentifier agent)
        {
            var defaultConfig = JsonSerializer.Serialize(new AgentConfiguration()
            {
                AgentId = agent
            });

            var secretName = GetConfigSecretName(agent);

            Pageable<SecretProperties> properties = null;
            try
            {
                properties = GetSecretClient().GetPropertiesOfSecretVersions(secretName);
            }
            catch { }

            if (properties == null || properties.AsPages().Count() > 0)
                await GetSecretClient().SetSecretAsync(secretName, defaultConfig);

            await _roleAssignment.ApplyScopedRoleToIdentity(agent, AssignableRole.CanReadWriteSecrets, GetSecretScope(secretName));
        }

        public Task OnAgentRemoved(ChimeraAgentIdentifier agent) =>
            GetSecretClient().StartDeleteSecretAsync(GetConfigSecretName(agent));
    }
}
