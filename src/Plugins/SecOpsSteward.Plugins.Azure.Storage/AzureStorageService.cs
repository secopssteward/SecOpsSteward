using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.Storage.Fluent;
using SecOpsSteward.Plugins.Configurable;
using SecOpsSteward.Plugins.Discovery;
using SecOpsSteward.Plugins.WorkflowTemplates;

namespace SecOpsSteward.Plugins.Azure.Storage
{
    public class AzureStorageServiceConfiguration : AzureSharedConfiguration, IConfigurableObjectConfiguration
    {
        [Required]
        [IdentifiesTargetGrantScope]
        [DisplayName("Storage Account Name")]
        public string StorageAccount { get; set; }

        internal string GetScope()
        {
            return "/subscriptions/" + SubscriptionId +
                   "/resourceGroups/" + ResourceGroup +
                   "/providers/Microsoft.Storage/storageAccounts/" + StorageAccount;
        }

        internal Task<IStorageAccount> GetStorageAsync(IAzure azure)
        {
            return azure.StorageAccounts.GetByResourceGroupAsync(ResourceGroup, StorageAccount);
        }
    }

    [ElementDescription(
        "Azure Storage",
        "Manages Azure Blob, Queue, and Table Storage")]
    public class AzureStorageService : SOSManagedService<AzureStorageServiceConfiguration>
    {
        public AzureStorageService(AzureCurrentCredentialFactory platformFactory)
        {
            PlatformFactory = platformFactory;
            if (platformFactory == null) throw new Exception("Platform handle not found");
        }

        public AzureStorageService()
        {
        }

        public override ManagedServiceRole Role => ManagedServiceRole.Producer;

        public override List<WorkflowTemplateDefinition> Templates => new()
        {
            TEMPLATE_RESET_KEY
        };

        private static WorkflowTemplateDefinition TEMPLATE_RESET_KEY =>
            new WorkflowTemplateDefinition<AzureStorageService, StorageRegenerateKeyConfiguration>(
                    "Reset Storage Account Key")
                .RunWorkflowStep<StorageRegenerateKey>(
                    nameof(StorageRegenerateKeyConfiguration.StorageAccount)
                        .MapsTo(nameof(StorageRegenerateKeyConfiguration.StorageAccount)));


        protected AzureCurrentCredentialFactory PlatformFactory { get; set; }

        public override async Task<List<DiscoveredServiceConfiguration>> Discover()
        {
            var azure = PlatformFactory.GetCredential(Configuration.SubscriptionId).GetAzure();

            IEnumerable<IStorageAccount> allStorage;
            if (string.IsNullOrEmpty(Configuration.ResourceGroup))
                allStorage = azure.StorageAccounts.List();
            else
                allStorage = azure.StorageAccounts.ListByResourceGroup(Configuration.ResourceGroup);

            return (await Task.WhenAll(allStorage.Select(async storage =>
            {
                await Task.Yield();
                return new DiscoveredServiceConfiguration
                {
                    ManagedServiceId = this.GenerateId(),
                    DescriptiveName = $"({this.GetDescriptiveName()}) {storage.ResourceGroupName} / {storage.Name}",
                    Configuration = new AzureStorageServiceConfiguration
                    {
                        TenantId = Configuration.TenantId,
                        SubscriptionId = Configuration.SubscriptionId,
                        ResourceGroup = storage.ResourceGroupName,
                        StorageAccount = storage.Name
                    },
                    Identifier = $"{storage.ResourceGroupName}/{storage.Name}",
                    LinksInAs = new List<string>
                    {
                        $"{storage.ResourceGroupName}/{storage.Name}",

                        storage.EndPoints?.Primary?.Blob,
                        storage.EndPoints?.Primary?.Dfs,
                        storage.EndPoints?.Primary?.File,
                        storage.EndPoints?.Primary?.Queue,
                        storage.EndPoints?.Primary?.Table,
                        storage.EndPoints?.Primary?.Web,

                        storage.EndPoints?.Secondary?.Blob,
                        storage.EndPoints?.Secondary?.Dfs,
                        storage.EndPoints?.Secondary?.File,
                        storage.EndPoints?.Secondary?.Queue,
                        storage.EndPoints?.Secondary?.Table,
                        storage.EndPoints?.Secondary?.Web
                    }
                };
            }))).ToList();
        }
    }
}