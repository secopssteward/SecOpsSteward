using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using SecOpsSteward.Plugins.Configurable;

namespace SecOpsSteward.Plugins.Azure.Storage
{
    public class StorageRegenerateKeyConfiguration : AzureStorageServiceConfiguration, IConfigurableObjectConfiguration
    {
        [DisplayName("Regenerate Primary Key?")]
        [Description("If true, the primary key will be regenerated. Otherwise the secondary key will be regenerated.")]
        public bool RegeneratePrimaryKey { get; set; }
    }

    [ElementDescription(
        "Regenerate Storage Account Key",
        "Anthony Turner",
        "Regenerates a Key for an Azure Storage Account",
        "1.0.0")]
    [ManagedService(typeof(AzureStorageService))]
    [PossibleResultCodes(CommonResultCodes.Success, CommonResultCodes.Failure)]
    [GeneratedSharedOutputs(
        "Storage/{{$Configuration.ResourceGroup}}/{{$Configuration.StorageAccount}}/{{$Configuration.RegeneratePrimaryKey?key1:key2}}")]
    public class StorageRegenerateKey : SOSPlugin<StorageRegenerateKeyConfiguration>
    {
        public StorageRegenerateKey(AzureCurrentCredentialFactory platformFactory)
        {
            PlatformFactory = platformFactory;
            if (platformFactory == null) throw new Exception("Platform handle not found");
        }

        public StorageRegenerateKey()
        {
        }

        protected AzureCurrentCredentialFactory PlatformFactory { get; set; }

        public override IEnumerable<PluginRbacRequirements> RbacRequirements => new[]
        {
            AzurePluginRbacRequirements.WithActions(
                "Read & Regenerate Storage Account Keys",
                Configuration.GetScope(),
                "Microsoft.Storage/storageAccounts/regenerateKey/action",
                "Microsoft.Storage/storageAccounts/read")
        };

        public override async Task<PluginOutputStructure> Execute(PluginOutputStructure previousOutput)
        {
            var azure = PlatformFactory.GetCredential(Configuration.SubscriptionId).GetAzure();
            var newKeys = await
                (await Configuration.GetStorageAsync(azure))
                .RegenerateKeyAsync(Configuration.RegeneratePrimaryKey ? "key1" : "key2");

            // todo: kerb1/kerb2

            return new PluginOutputStructure(CommonResultCodes.Success)
                .WithSecureOutput(
                    $"Storage/{Configuration.ResourceGroup}/{Configuration.StorageAccount}/{(Configuration.RegeneratePrimaryKey ? "key1" : "key2")}",
                    newKeys[0].Value);
        }
    }
}