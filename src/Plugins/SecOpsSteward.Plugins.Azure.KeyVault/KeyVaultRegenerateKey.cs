using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Management.KeyVault.Fluent.Models;

namespace SecOpsSteward.Plugins.Azure.KeyVault
{
    public class KeyVaultRegenerateKeyConfiguration : AzureKeyVaultServiceConfiguration
    {
        [Required] [DisplayName("Key Name")] public string KeyName { get; set; }
    }

    [ElementDescription(
        "Regenerate Key Vault Key",
        "Anthony Turner",
        "Regenerates a Key Vault Key with the same attributes",
        "1.0.0")]
    [ManagedService(typeof(AzureKeyVaultService))]
    [PossibleResultCodes(CommonResultCodes.Success, CommonResultCodes.Failure)]
    public class KeyVaultRegenerateKey : SOSPlugin<KeyVaultRegenerateKeyConfiguration>
    {
        public KeyVaultRegenerateKey(AzureCurrentCredentialFactory platformFactory)
        {
            PlatformFactory = platformFactory;
            if (platformFactory == null) throw new Exception("Platform handle not found");
        }

        public KeyVaultRegenerateKey()
        {
        }

        protected AzureCurrentCredentialFactory PlatformFactory { get; set; }

        public override IEnumerable<PluginRbacRequirements> RbacRequirements => new[]
        {
            AzurePluginRbacRequirements.WithActions(
                "Read & Write Key Vault Keys",
                Configuration.GetScope(),
                "Microsoft.KeyVault/vaults/keys/read",
                "Microsoft.KeyVault/vaults/keys/write")
        };

        public override async Task<PluginOutputStructure> Execute(PluginOutputStructure previousOutput)
        {
            var azure = PlatformFactory.GetCredential(Configuration.SubscriptionId).GetAzure();
            var vault = await Configuration.GetVaultAsync(azure);

            var keyObj = await vault.Keys.GetByNameAsync(Configuration.KeyName);
            await vault.Keys.Define(Configuration.KeyName)
                .WithKeyTypeToCreate(JsonWebKeyType.Rsa) // how to get this???
                .WithKeyOperations(keyObj.JsonWebKey.KeyOps.Select(o => JsonWebKeyOperation.Parse(o)).ToList())
                .WithAttributes(keyObj.Attributes)
                .WithKeySize(2048) // how to get this???
                .WithTags(keyObj.Tags as IDictionary<string, string>)
                .CreateAsync();

            return new PluginOutputStructure(CommonResultCodes.Success);
        }
    }
}