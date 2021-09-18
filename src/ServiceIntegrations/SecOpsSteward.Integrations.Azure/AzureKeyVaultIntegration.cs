using System;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;
using SecOpsSteward.Plugins.Azure;
using SecOpsSteward.Shared;
using SecOpsSteward.Shared.Roles;

namespace SecOpsSteward.Integrations.Azure
{
    public class AzureKeyVaultIntegration : AzurePlatformIntegrationBase
    {
        internal static readonly KeyType KEY_TYPE = KeyType.Rsa;
        internal static readonly KeyWrapAlgorithm KEY_WRAP_ALGORITHM = KeyWrapAlgorithm.Rsa15;
        internal static readonly SignatureAlgorithm SIGNATURE_ALGORITHM = SignatureAlgorithm.RS256;
        internal static readonly EncryptionAlgorithm ASYMMETRIC_ENCRYPTION_ALGORITHM = EncryptionAlgorithm.Rsa15;

        protected AzureKeyVaultIntegration(
            ILogger<AzureKeyVaultIntegration> logger,
            ChimeraServiceConfigurator configurator,
            IRoleAssignmentService roleAssignment,
            AzureCurrentCredentialFactory platformFactory) : base(logger, configurator, roleAssignment, platformFactory)
        {
        }

        private string AgentVaultName => _configurator["AgentVaultName"];
        private string UserVaultName => _configurator["UserVaultName"];

        protected string AgentVaultScope => $"{BaseScope}/providers/Microsoft.KeyVault/vaults/" + AgentVaultName;
        protected string UserVaultScope => $"{BaseScope}/providers/Microsoft.KeyVault/vaults/" + UserVaultName;

        protected static string GetConfigSecretName(ChimeraAgentIdentifier identifier)
        {
            return $"{identifier}-config";
        }

        protected static string GetKeyNameFromIdentifier(ChimeraEntityIdentifier identifier)
        {
            return identifier.Type switch
            {
                ChimeraEntityIdentifier.EntityType.Agent => "agent-" + identifier.Id,
                ChimeraEntityIdentifier.EntityType.User => "user-" + identifier.Id,
                _ => throw new Exception("Invalid identifier type")
            };
        }

        protected string GetKeyIdFromIdentifier(ChimeraEntityIdentifier identifier)
        {
            // todo: can we simplify this to generating a URL?
            var keyName = GetKeyNameFromIdentifier(identifier);
            if (identifier is ChimeraUserIdentifier)
                return GetUserKeyClient().GetKey(keyName).Value.Id.ToString();
            else
                return GetAgentKeyClient().GetKey(keyName).Value.Id.ToString();
        }

        protected CryptographyClient GetCryptoClient(ChimeraEntityIdentifier identifier)
        {
            return new(new Uri(GetKeyIdFromIdentifier(identifier)), _platformFactory.GetCredential().Credential);
        }

        protected KeyClient GetUserKeyClient()
        {
            return new(new Uri($"https://{UserVaultName}.vault.azure.net"), _platformFactory.GetCredential().Credential);
        }

        protected KeyClient GetAgentKeyClient()
        {
            return new(new Uri($"https://{AgentVaultName}.vault.azure.net"), _platformFactory.GetCredential().Credential);
        }

        protected SecretClient GetUserSecretClient()
        {
            return new(new Uri($"https://{UserVaultName}.vault.azure.net"), _platformFactory.GetCredential().Credential);
        }

        protected SecretClient GetAgentSecretClient()
        {
            return new(new Uri($"https://{AgentVaultName}.vault.azure.net"), _platformFactory.GetCredential().Credential);
        }

        protected string GetSecretScope(string secretName)
        {
            return $"{AgentVaultScope}/secrets/{secretName}";
        }

        protected string GetAgentKeyScope(string keyName)
        {
            return $"{AgentVaultScope}/keys/{keyName}";
        }

        protected string GetUserKeyScope(string keyName)
        {
            return $"{UserVaultScope}/keys/{keyName}";
        }
    }
}