using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.KeyVault.Fluent;
using SecOpsSteward.Plugins.Configurable;
using SecOpsSteward.Plugins.Discovery;
using SecOpsSteward.Plugins.WorkflowTemplates;

namespace SecOpsSteward.Plugins.Azure.KeyVault
{
    public class AzureKeyVaultServiceConfiguration : AzureSharedConfiguration, IConfigurableObjectConfiguration
    {
        [Required]
        [DisplayName("Key Vault Name")]
        [IdentifiesTargetGrantScope]
        public string KeyVault { get; set; }

        internal string GetScope()
        {
            return "/subscriptions/" + SubscriptionId +
                   "/resourceGroups/" + ResourceGroup +
                   "/providers/Microsoft.KeyVault/vaults/" + KeyVault;
        }

        internal Task<IVault> GetVaultAsync(IAzure azure)
        {
            return azure.Vaults.GetByResourceGroupAsync(ResourceGroup, KeyVault);
        }
    }

    [ElementDescription(
        "Azure Key Vault",
        "Manages Azure Key Vaults")]
    public class AzureKeyVaultService : SOSManagedService<AzureKeyVaultServiceConfiguration>
    {
        public AzureKeyVaultService(AzureCurrentCredentialFactory platformFactory)
        {
            PlatformFactory = platformFactory;
            if (platformFactory == null) throw new Exception("Platform handle not found");
        }

        public AzureKeyVaultService()
        {
        }

        public override ManagedServiceRole Role => ManagedServiceRole.Hybrid;

        public override List<WorkflowTemplateDefinition> Templates => new()
        {
            TEMPLATE_RESET_KEY,
            TEMPLATE_RESET_SECRET
        };

        private WorkflowTemplateDefinition TEMPLATE_RESET_KEY =>
            new WorkflowTemplateDefinition<AzureKeyVaultService, KeyVaultRegenerateKeyConfiguration>(
                    "Reset Key Vault Key")
                .RunWorkflowStep<KeyVaultRegenerateKey>(
                    nameof(KeyVaultRegenerateKeyConfiguration.KeyName)
                        .MapsTo(nameof(KeyVaultRegenerateKeyConfiguration.KeyName)));


        private WorkflowTemplateDefinition TEMPLATE_RESET_SECRET =>
            new WorkflowTemplateDefinition<AzureKeyVaultService, KeyVaultRegenerateSecretConfiguration>(
                    "Reset Key Vault Secret")
                .RunWorkflowStep<KeyVaultRegenerateSecret>(
                    nameof(KeyVaultRegenerateSecretConfiguration.SecretName)
                        .MapsTo(nameof(KeyVaultRegenerateSecretConfiguration.SecretName)),
                    nameof(KeyVaultRegenerateSecretConfiguration.Length)
                        .MapsTo(nameof(KeyVaultRegenerateSecretConfiguration.Length)))
                .RunAnyChildWorkflows();


        protected AzureCurrentCredentialFactory PlatformFactory { get; set; }

        public override async Task<List<DiscoveredServiceConfiguration>> Discover()
        {
            await Task.Yield();
            var azure = PlatformFactory.GetCredential(Configuration.SubscriptionId).GetAzure();

            IEnumerable<IVault> allVaults;
            if (string.IsNullOrEmpty(Configuration.ResourceGroup))
                allVaults = azure.Vaults.List();
            else
                allVaults = azure.Vaults.ListByResourceGroup(Configuration.ResourceGroup);

            return allVaults.Select(app =>
            {
                return new DiscoveredServiceConfiguration
                {
                    ManagedServiceId = this.GenerateId(),
                    DescriptiveName = $"({this.GetDescriptiveName()}) {app.ResourceGroupName} / {app.Name}",
                    Configuration = new AzureKeyVaultServiceConfiguration
                    {
                        TenantId = Configuration.TenantId,
                        SubscriptionId = Configuration.SubscriptionId,
                        ResourceGroup = app.ResourceGroupName,
                        KeyVault = app.Name
                    },
                    Identifier = $"{app.ResourceGroupName}/{app.Name}",
                    LinksInAs = new List<string>
                    {
                        $"{app.ResourceGroupName}/{app.Name}",
                        app.VaultUri
                    }
                };
            }).ToList();
        }
    }
}