using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SecOpsSteward.Plugins.Azure;
using SecOpsSteward.Shared;
using SecOpsSteward.Shared.Cryptography.Extensions;
using SecOpsSteward.Shared.Roles;
using SecOpsSteward.Shared.Services;

namespace SecOpsSteward.Integrations.Azure.Cryptography
{
    public class AzureKeyVaultCryptographicService : AzureKeyVaultIntegration, ICryptographicService,
        IHasUserEnrollmentActions, IHasAgentCreationActions
    {
        public AzureKeyVaultCryptographicService(
            ILogger<AzureKeyVaultCryptographicService> logger,
            ChimeraServiceConfigurator configurator,
            IRoleAssignmentService roleAssignment,
            AzureCurrentCredentialFactory platformFactory) : base(logger, configurator, roleAssignment, platformFactory)
        {
        }

        public async Task<byte[]> Decrypt(ChimeraEntityIdentifier key, byte[] ciphertext)
        {
            return (await GetCryptoClient(key).DecryptAsync(ASYMMETRIC_ENCRYPTION_ALGORITHM, ciphertext)).Plaintext;
        }

        public async Task<byte[]> Encrypt(ChimeraEntityIdentifier key, byte[] data)
        {
            return (await GetCryptoClient(key).EncryptAsync(ASYMMETRIC_ENCRYPTION_ALGORITHM, data)).Ciphertext;
        }

        public async Task<byte[]> Sign(ChimeraEntityIdentifier signer, byte[] digest)
        {
            return (await GetCryptoClient(signer).SignAsync(SIGNATURE_ALGORITHM, digest)).Signature;
        }

        public async Task<byte[]> UnwrapKey(ChimeraEntityIdentifier wrappingKey, byte[] keyToUnwrap)
        {
            return (await GetCryptoClient(wrappingKey).UnwrapKeyAsync(KEY_WRAP_ALGORITHM, keyToUnwrap)).Key;
        }

        public async Task<bool> Verify(ChimeraEntityIdentifier signer, byte[] signature, byte[] digest)
        {
            return (await GetCryptoClient(signer).VerifyAsync(SIGNATURE_ALGORITHM, digest, signature)).IsValid;
        }

        public async Task<byte[]> WrapKey(ChimeraEntityIdentifier wrappingKey, byte[] keyToWrap)
        {
            return (await GetCryptoClient(wrappingKey).WrapKeyAsync(KEY_WRAP_ALGORITHM, keyToWrap)).EncryptedKey;
        }

        public Task OnAgentCreated(ChimeraAgentIdentifier agent)
        {
            return CreateKeysFor(agent);
        }

        public Task OnAgentRemoved(ChimeraAgentIdentifier agent)
        {
            return GetAgentKeyClient().StartDeleteKeyAsync(GetKeyNameFromIdentifier(agent));
        }

        public int ServicePriority => 20;

        // ---

        public async Task OnUserEnrolled(ChimeraUserIdentifier user, ChimeraUserRole role)
        {
            await CreateKeysFor(user);

            if (role.HasFlag(ChimeraUserRole.AgentAdmin))
            {
                await _roleAssignment.ApplyScopedRoleToIdentity(user, AssignableRole.CanCreateKeys, AgentVaultScope);
                await _roleAssignment.ApplyScopedRoleToIdentity(user, AssignableRole.CanManagePermissionsOnVault, AgentVaultScope);
            }

            if (role.HasFlag(ChimeraUserRole.UserAdmin))
            {
                await _roleAssignment.ApplyScopedRoleToIdentity(user, AssignableRole.CanCreateKeys, UserVaultScope);
                await _roleAssignment.ApplyScopedRoleToIdentity(user, AssignableRole.CanManagePermissionsOnVault, UserVaultScope);
            }
        }

        public async Task OnUserRemoved(ChimeraUserIdentifier user, ChimeraUserRole role)
        {
            await GetUserKeyClient().StartDeleteKeyAsync(GetKeyNameFromIdentifier(user));

            if (role.HasFlag(ChimeraUserRole.AgentAdmin))
            {
                await _roleAssignment.RemoveScopedRoleFromIdentity(user, AssignableRole.CanCreateKeys, AgentVaultScope);
                await _roleAssignment.RemoveScopedRoleFromIdentity(user, AssignableRole.CanManagePermissionsOnVault, AgentVaultScope);
            }

            if (role.HasFlag(ChimeraUserRole.UserAdmin))
            {
                await _roleAssignment.RemoveScopedRoleFromIdentity(user, AssignableRole.CanCreateKeys, UserVaultScope);
                await _roleAssignment.RemoveScopedRoleFromIdentity(user, AssignableRole.CanManagePermissionsOnVault, UserVaultScope);
            }
        }

        public async Task CreateKeysFor(ChimeraEntityIdentifier entity)
        {
            Logger.LogTrace($"Starting creation of keys for {entity}");
            // Create UID keypair, grant Sign/Decrypt
            var keyName = GetKeyNameFromIdentifier(entity);

            try
            {
                if (entity is ChimeraUserIdentifier)
                    await GetUserKeyClient().GetKeyAsync(keyName);
                else
                    await GetAgentKeyClient().GetKeyAsync(keyName);
                Logger.LogTrace($"{entity} keys already exist");
            }
            catch
            {
                if (entity is ChimeraUserIdentifier)
                    await GetUserKeyClient().CreateKeyAsync(keyName, KEY_TYPE);
                else
                    await GetAgentKeyClient().CreateKeyAsync(keyName, KEY_TYPE);

                Logger.LogTrace($"{entity} keys successfully created");
            }

            var vaultScope = entity is ChimeraUserIdentifier ? UserVaultScope : AgentVaultScope;
            var keyScope = entity is ChimeraUserIdentifier ? GetUserKeyScope(keyName) : GetAgentKeyScope(keyName);

            await _roleAssignment.ApplyScopedRoleToIdentity(entity, AssignableRole.CanSignDecryptKey, keyScope);
            await _roleAssignment.ApplyScopedRoleToIdentity(entity, AssignableRole.CanValidateEncryptKey, vaultScope);
        }
    }
}