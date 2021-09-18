using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace SecOpsSteward.Plugins.Azure.KeyVault
{
    public class KeyVaultRegenerateSecretConfiguration : AzureKeyVaultServiceConfiguration
    {
        [Required]
        [DisplayName("Secret Length")]
        [Description("Length of newly generated secret")]
        public int Length { get; set; } = 64;

        [Required]
        [DisplayName("Secret Name")]
        public string SecretName { get; set; }
    }

    [ElementDescription(
        "Regenerate Key Vault Secret",
        "Anthony Turner",
        "Updates a Key Vault Secret with a new random string",
        "1.0.0")]
    [ManagedService(typeof(AzureKeyVaultService))]
    [PossibleResultCodes(CommonResultCodes.Success, CommonResultCodes.Failure)]
    [GeneratedSharedOutputs(
        "KeyVault/{{$Configuration.ResourceGroup}}/{{$Configuration.KeyVault}}/{{$Configuration.SecretName}}")]
    public class KeyVaultRegenerateSecret : SOSPlugin<KeyVaultRegenerateSecretConfiguration>
    {
        public KeyVaultRegenerateSecret(AzureCurrentCredentialFactory platformFactory)
        {
            PlatformFactory = platformFactory;
            if (platformFactory == null) throw new Exception("Platform handle not found");
        }

        public KeyVaultRegenerateSecret()
        {
        }

        protected AzureCurrentCredentialFactory PlatformFactory { get; set; }

        public override IEnumerable<PluginRbacRequirements> RbacRequirements => new[]
        {
            AzurePluginRbacRequirements.WithActions(
                "Read & Write Key Vault Secrets",
                Configuration.GetScope(),
                "Microsoft.KeyVault/vaults/secrets/read",
                "Microsoft.KeyVault/vaults/secrets/write")
        };

        public override async Task<PluginOutputStructure> Execute(PluginOutputStructure previousOutput)
        {
            var azure = PlatformFactory.GetCredential(Configuration.SubscriptionId).GetAzure();
            var vault = await Configuration.GetVaultAsync(azure);

            var secretObj = await vault.Secrets.GetByNameAsync(Configuration.SecretName);

            var newValue = PluginSharedHelpers.RandomString(Configuration.Length);
            await vault.Secrets.Define(Configuration.SecretName)
                .WithValue(newValue)
                .WithAttributes(secretObj.Attributes)
                .WithContentType(secretObj.ContentType)
                .WithTags(secretObj.Tags as IDictionary<string, string>)
                .CreateAsync();

            return new PluginOutputStructure(CommonResultCodes.Success)
                .WithSecureOutput(
                    $"KeyVault/{Configuration.ResourceGroup}/{Configuration.KeyVault}/{Configuration.SecretName}",
                    newValue);
        }
    }
}