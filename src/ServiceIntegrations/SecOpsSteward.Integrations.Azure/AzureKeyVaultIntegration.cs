using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;
using SecOpsSteward.Plugins.Azure;
using SecOpsSteward.Shared;
using SecOpsSteward.Shared.Roles;
using System;

namespace SecOpsSteward.Integrations.Azure
{
    public class AzureKeyVaultIntegration : AzurePlatformIntegrationBase
    {
        internal static readonly KeyType KEY_TYPE = KeyType.Rsa;
        internal static readonly KeyWrapAlgorithm KEY_WRAP_ALGORITHM = KeyWrapAlgorithm.Rsa15;
        internal static readonly SignatureAlgorithm SIGNATURE_ALGORITHM = SignatureAlgorithm.RS256;
        internal static readonly EncryptionAlgorithm ASYMMETRIC_ENCRYPTION_ALGORITHM = EncryptionAlgorithm.Rsa15;

        private string CfgVaultName => _configurator["VaultName"];

        protected AzureKeyVaultIntegration(
            ILogger<AzureKeyVaultIntegration> logger,
            ChimeraServiceConfigurator configurator,
            IRoleAssignmentService roleAssignment,
            AzureCurrentCredentialFactory platformFactory) : base(logger, configurator, roleAssignment, platformFactory) { }

        protected static string GetConfigSecretName(ChimeraAgentIdentifier identifier) => $"{identifier}-config";

        protected static string GetKeyNameFromIdentifier(ChimeraEntityIdentifier identifier)
        {
            return identifier.Type switch
            {
                ChimeraEntityIdentifier.EntityType.Agent => "agent-" + identifier.Id,
                ChimeraEntityIdentifier.EntityType.User => "user-" + identifier.Id,
                _ => throw new Exception("Invalid identifier type"),
            };
        }
        protected string GetKeyIdFromIdentifier(ChimeraEntityIdentifier identifier)
        {
            // todo: can we simplify this to generating a URL?
            var keyName = GetKeyNameFromIdentifier(identifier);
            return GetKeyClient().GetKey(keyName).Value.Id.ToString();
        }

        protected CryptographyClient GetCryptoClient(ChimeraEntityIdentifier identifier) =>
            new CryptographyClient(new Uri(GetKeyIdFromIdentifier(identifier)), _platformFactory.GetCredential().Credential);
        protected KeyClient GetKeyClient() =>
            new KeyClient(new Uri($"https://{CfgVaultName}.vault.azure.net"), _platformFactory.GetCredential().Credential);
        protected SecretClient GetSecretClient() =>
            new SecretClient(new Uri($"https://{CfgVaultName}.vault.azure.net"), _platformFactory.GetCredential().Credential);

        protected string VaultScope => $"{BaseScope}/providers/Microsoft.KeyVault/vaults/" + CfgVaultName;

        protected string GetSecretScope(string secretName) => $"{VaultScope}/secrets/{secretName}";
        protected string GetKeyScope(string keyName) => $"{VaultScope}/keys/{keyName}";
    }
}
