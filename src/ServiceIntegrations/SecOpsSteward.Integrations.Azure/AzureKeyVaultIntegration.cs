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

        private string CfgVaultName => _configurator["VaultName"];

        protected string VaultScope => $"{BaseScope}/providers/Microsoft.KeyVault/vaults/" + CfgVaultName;

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
            return GetKeyClient().GetKey(keyName).Value.Id.ToString();
        }

        protected CryptographyClient GetCryptoClient(ChimeraEntityIdentifier identifier)
        {
            return new(new Uri(GetKeyIdFromIdentifier(identifier)), _platformFactory.GetCredential().Credential);
        }

        protected KeyClient GetKeyClient()
        {
            return new(new Uri($"https://{CfgVaultName}.vault.azure.net"), _platformFactory.GetCredential().Credential);
        }

        protected SecretClient GetSecretClient()
        {
            return new(new Uri($"https://{CfgVaultName}.vault.azure.net"), _platformFactory.GetCredential().Credential);
        }

        protected string GetSecretScope(string secretName)
        {
            return $"{VaultScope}/secrets/{secretName}";
        }

        protected string GetKeyScope(string keyName)
        {
            return $"{VaultScope}/keys/{keyName}";
        }
    }
}