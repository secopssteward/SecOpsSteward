using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Azure;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;
using SecOpsSteward.Plugins.Azure;
using SecOpsSteward.Shared;
using SecOpsSteward.Shared.Configuration;
using SecOpsSteward.Shared.Roles;
using SecOpsSteward.Shared.Services;

namespace SecOpsSteward.Integrations.Azure.Configuration
{
    public class AzureKeyVaultConfigurationService : AzureKeyVaultIntegration, IConfigurationProvider,
        IHasAgentCreationActions, IHasUserEnrollmentActions
    {
        public AzureKeyVaultConfigurationService(
            ILogger<AzureKeyVaultConfigurationService> logger,
            ChimeraServiceConfigurator configurator,
            IRoleAssignmentService roleAssignment,
            AzureCurrentCredentialFactory platformFactory) : base(logger, configurator, roleAssignment, platformFactory)
        {
        }

        public async Task<AgentConfiguration> GetConfiguration(ChimeraAgentIdentifier agent)
        {
            var secret = (await GetAgentSecretClient().GetSecretAsync(GetConfigSecretName(agent))).Value;
            return ChimeraSharedHelpers.GetFromSerializedString<AgentConfiguration>(secret.Value);
        }

        public async Task<List<AgentConfiguration>> ListConfigurations()
        {
            var secrets = GetAgentSecretClient().GetPropertiesOfSecrets().Where(s => s.Name.EndsWith("-config"));
            return (await Task.WhenAll(secrets.Select(async s =>
            {
                var configStr = await GetAgentSecretClient().GetSecretAsync(s.Name);
                return ChimeraSharedHelpers.GetFromSerializedString<AgentConfiguration>(configStr.Value.Value);
            }))).ToList();
        }

        public async Task UpdateConfiguration(ChimeraAgentIdentifier agent, AgentConfiguration configuration)
        {
            var value = ChimeraSharedHelpers.SerializeToString(configuration);
            await GetAgentSecretClient().SetSecretAsync(GetConfigSecretName(agent), value);
        }

        public int ServicePriority => 20;

        // ---

        public async Task OnAgentCreated(ChimeraAgentIdentifier agent)
        {
            var defaultConfig = JsonSerializer.Serialize(new AgentConfiguration
            {
                AgentId = agent
            });

            var secretName = GetConfigSecretName(agent);

            Pageable<SecretProperties> properties = null;
            try
            {
                properties = GetAgentSecretClient().GetPropertiesOfSecretVersions(secretName);
            }
            catch
            {
            }

            if (properties == null || properties.AsPages().Count() > 0)
                await GetAgentSecretClient().SetSecretAsync(secretName, defaultConfig);

            await _roleAssignment.ApplyScopedRoleToIdentity(agent, AssignableRole.CanReadWriteSecrets,
                GetSecretScope(secretName));
        }

        public Task OnAgentRemoved(ChimeraAgentIdentifier agent)
        {
            return GetAgentSecretClient().StartDeleteSecretAsync(GetConfigSecretName(agent));
        }

        public async Task OnUserEnrolled(ChimeraUserIdentifier user, ChimeraUserRole userRole)
        {
            if (userRole.HasFlag(ChimeraUserRole.AgentAdmin))
                await _roleAssignment.ApplyScopedRoleToIdentity(user, AssignableRole.CanReadWriteSecrets, AgentVaultScope);
        }

        public async Task OnUserRemoved(ChimeraUserIdentifier user, ChimeraUserRole userRole)
        {
            if (userRole.HasFlag(ChimeraUserRole.AgentAdmin))
                await _roleAssignment.ApplyScopedRoleToIdentity(user, AssignableRole.CanReadWriteSecrets, AgentVaultScope);
        }
    }
}