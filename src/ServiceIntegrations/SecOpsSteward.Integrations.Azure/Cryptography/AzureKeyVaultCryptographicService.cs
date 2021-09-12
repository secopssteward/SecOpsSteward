using Microsoft.Extensions.Logging;
using SecOpsSteward.Plugins.Azure;
using SecOpsSteward.Shared;
using SecOpsSteward.Shared.Cryptography;
using SecOpsSteward.Shared.Roles;
using SecOpsSteward.Shared.Services;
using System.Threading.Tasks;

namespace SecOpsSteward.Integrations.Azure.Cryptography
{
    public class AzureKeyVaultCryptographicService : AzureKeyVaultIntegration, ICryptographicService, IHasUserEnrollmentActions, IHasAgentCreationActions
    {
        public int ServicePriority => 20;

        public AzureKeyVaultCryptographicService(
            ILogger<AzureKeyVaultCryptographicService> logger,
            ChimeraServiceConfigurator configurator,
            IRoleAssignmentService roleAssignment,
            AzureCurrentCredentialFactory platformFactory) : base(logger, configurator, roleAssignment, platformFactory) { }

        public async Task<byte[]> Decrypt(ChimeraEntityIdentifier key, byte[] ciphertext) =>
            (await GetCryptoClient(key).DecryptAsync(ASYMMETRIC_ENCRYPTION_ALGORITHM, ciphertext)).Plaintext;

        public async Task<byte[]> Encrypt(ChimeraEntityIdentifier key, byte[] data) =>
            (await GetCryptoClient(key).EncryptAsync(ASYMMETRIC_ENCRYPTION_ALGORITHM, data)).Ciphertext;

        public async Task<byte[]> Sign(ChimeraEntityIdentifier signer, byte[] digest) =>
            (await GetCryptoClient(signer).SignAsync(SIGNATURE_ALGORITHM, digest)).Signature;

        public async Task<byte[]> UnwrapKey(ChimeraEntityIdentifier wrappingKey, byte[] keyToUnwrap) =>
            (await GetCryptoClient(wrappingKey).UnwrapKeyAsync(KEY_WRAP_ALGORITHM, keyToUnwrap)).Key;

        public async Task<bool> Verify(ChimeraEntityIdentifier signer, byte[] signature, byte[] digest) =>
            (await GetCryptoClient(signer).VerifyAsync(SIGNATURE_ALGORITHM, digest, signature)).IsValid;

        public async Task<byte[]> WrapKey(ChimeraEntityIdentifier wrappingKey, byte[] keyToWrap) =>
            (await GetCryptoClient(wrappingKey).WrapKeyAsync(KEY_WRAP_ALGORITHM, keyToWrap)).EncryptedKey;

        // ---

        public Task OnUserEnrolled(ChimeraUserIdentifier user) => CreateKeysFor(user);

        public Task OnUserRemoved(ChimeraUserIdentifier user) => GetKeyClient().StartDeleteKeyAsync(GetKeyNameFromIdentifier(user));

        public Task OnAgentCreated(ChimeraAgentIdentifier agent) => CreateKeysFor(agent);

        public Task OnAgentRemoved(ChimeraAgentIdentifier agent) => GetKeyClient().StartDeleteKeyAsync(GetKeyNameFromIdentifier(agent));

        public async Task CreateKeysFor(ChimeraEntityIdentifier entity)
        {
            Logger.LogTrace($"Starting creation of keys for {entity}");
            // Create UID keypair, grant Sign/Decrypt
            var keyName = GetKeyNameFromIdentifier(entity);

            try
            {
                var thisKey = GetKeyClient().GetKeyAsync(keyName).Result;
                Logger.LogTrace($"{entity} keys successfully created");
            }
            catch
            {
                Logger.LogTrace($"{entity} keys already exist");
                await GetKeyClient().CreateKeyAsync(keyName, KEY_TYPE);
            }

            await _roleAssignment.ApplyScopedRoleToIdentity(entity, AssignableRole.CanSignDecryptKey, GetKeyScope(keyName));
            await _roleAssignment.ApplyScopedRoleToIdentity(entity, AssignableRole.CanValidateEncryptKey, VaultScope);
        }
    }
}
