using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.Fluent;
using SecOpsSteward.Plugins.Configurable;
using SecOpsSteward.Plugins.Discovery;
using SecOpsSteward.Plugins.WorkflowTemplates;

namespace SecOpsSteward.Plugins.Azure.AppServices.Functions
{
    public class AzureFunctionServiceConfiguration : AzureSharedConfiguration, IConfigurableObjectConfiguration
    {
        [Required]
        [IdentifiesTargetGrantScope]
        [DisplayName("Function App")]
        public string FunctionAppName { get; set; }

        internal string GetScope()
        {
            return "/subscriptions/" + SubscriptionId +
                   "/resourceGroups/" + ResourceGroup +
                   "/providers/Microsoft.Web/sites/" + FunctionAppName;
        }

        internal Task<IFunctionApp> GetAppAsync(IAzure azure)
        {
            return azure.AppServices.FunctionApps.GetByResourceGroupAsync(ResourceGroup, FunctionAppName);
        }
    }

    public class SlotSwapWorkflowConfiguration : AzureFunctionServiceConfiguration
    {
        [Required]
        [DisplayName("Original Slot")]
        [Description("Slot which contains the application to receive the updated configuration")]
        public string OriginalSlot { get; set; } = "production";

        [Required]
        [DisplayName("Temporary Slot")]
        [Description("Slot which will be used temporarily, to reduce from changes via slot swapping")]
        public string TemporarySlot { get; set; } = "temp-keyswap";
    }

    public class SlotSwapWithConfigWorkflowConfiguration : SlotSwapWorkflowConfiguration
    {
        [Required]
        [DisplayName("Configuration Item Name")]
        public string Name { get; set; }

        [Required]
        [DisplayName("Configuration Item Value")]
        public string Value { get; set; }
    }

    [ElementDescription(
        "Azure Functions",
        "Manages Azure Functions apps")]
    public class AzureFunctionService : SOSManagedService<AzureFunctionServiceConfiguration>
    {
        public AzureFunctionService(AzureCurrentCredentialFactory platformFactory)
        {
            PlatformFactory = platformFactory;
            if (platformFactory == null) throw new Exception("Platform handle not found");
        }

        public AzureFunctionService()
        {
        }

        public override ManagedServiceRole Role => ManagedServiceRole.Hybrid;

        public override List<WorkflowTemplateDefinition> Templates => new()
        {
            TEMPLATE_SWAP_OUT_BACK,
            TEMPLATE_SWAP_OUT_BACK_WITH_CONFIG_CHANGE,
            TEMPLATE_RESET_KEY,
            TEMPLATE_SET_CONFIG
        };

        private static WorkflowTemplateDefinition TEMPLATE_SWAP_OUT_BACK =>
            new WorkflowTemplateDefinition<AzureFunctionService, SlotSwapWorkflowConfiguration>("Perform Slot Swap")
                .RunWorkflowStep<AzureFunctionSlotSwap>(
                    nameof(SlotSwapWorkflowConfiguration.OriginalSlot)
                        .MapsTo(nameof(AzureFunctionSlotSwapConfiguration.SourceSlot)),
                    nameof(SlotSwapWorkflowConfiguration.TemporarySlot)
                        .MapsTo(nameof(AzureFunctionSlotSwapConfiguration.DestinationSlot)))
                .RunAnyChildWorkflows()
                .RunWorkflowStep<AzureFunctionSlotSwap>(
                    nameof(SlotSwapWorkflowConfiguration.TemporarySlot)
                        .MapsTo(nameof(AzureFunctionSlotSwapConfiguration.SourceSlot)),
                    nameof(SlotSwapWorkflowConfiguration.OriginalSlot)
                        .MapsTo(nameof(AzureFunctionSlotSwapConfiguration.DestinationSlot)));


        private static WorkflowTemplateDefinition TEMPLATE_SWAP_OUT_BACK_WITH_CONFIG_CHANGE =>
            new WorkflowTemplateDefinition<AzureFunctionService, SlotSwapWorkflowConfiguration>(
                    "Perform Slot Swap With Configuration Update")
                .RunWorkflowStep<AzureFunctionSlotSwap>(
                    nameof(SlotSwapWorkflowConfiguration.OriginalSlot)
                        .MapsTo(nameof(AzureFunctionSlotSwapConfiguration.SourceSlot)),
                    nameof(SlotSwapWorkflowConfiguration.TemporarySlot)
                        .MapsTo(nameof(AzureFunctionSlotSwapConfiguration.DestinationSlot)))
                .RunAnyChildWorkflows()
                .RunWorkflowStep<SetFunctionConfiguration>()
                .RunWorkflowStep<AzureFunctionSlotSwap>(
                    nameof(SlotSwapWorkflowConfiguration.TemporarySlot)
                        .MapsTo(nameof(AzureFunctionSlotSwapConfiguration.SourceSlot)),
                    nameof(SlotSwapWorkflowConfiguration.OriginalSlot)
                        .MapsTo(nameof(AzureFunctionSlotSwapConfiguration.DestinationSlot)));


        private static WorkflowTemplateDefinition TEMPLATE_RESET_KEY =>
            new WorkflowTemplateDefinition<AzureFunctionService, AzureFunctionKeyResetConfiguration>(
                    "Reset Function Key")
                .RunWorkflowStep<AzureFunctionKeyReset>(
                    nameof(AzureFunctionKeyResetConfiguration.FunctionKeyName)
                        .MapsTo(nameof(AzureFunctionKeyResetConfiguration.FunctionKeyName)));


        private static WorkflowTemplateDefinition TEMPLATE_SET_CONFIG =>
            new WorkflowTemplateDefinition<AzureFunctionService, SetFunctionConfigurationConfiguration>(
                    "Set Function Configuration")
                .RunWorkflowStep<SetFunctionConfiguration>(
                    nameof(SetFunctionConfigurationConfiguration.FunctionAppName)
                        .MapsTo(nameof(SetFunctionConfigurationConfiguration.FunctionAppName)),
                    nameof(SetFunctionConfigurationConfiguration.Name)
                        .MapsTo(nameof(SetFunctionConfigurationConfiguration.Name)),
                    nameof(SetFunctionConfigurationConfiguration.Value)
                        .MapsTo(nameof(SetFunctionConfigurationConfiguration.Value)));


        protected AzureCurrentCredentialFactory PlatformFactory { get; set; }

        public override async Task<List<DiscoveredServiceConfiguration>> Discover()
        {
            var azure = PlatformFactory.GetCredential(Configuration.SubscriptionId).GetAzure();

            IEnumerable<IFunctionApp> apps;
            if (string.IsNullOrEmpty(Configuration.ResourceGroup))
                apps = azure.AppServices.FunctionApps.List();
            else
                apps = azure.AppServices.FunctionApps.ListByResourceGroup(Configuration.ResourceGroup);

            return (await Task.WhenAll(apps.Select(async app =>
            {
                var settings = await app.GetAppSettingsAsync();
                var connstr = await app.GetConnectionStringsAsync();

                var c = new DiscoveredServiceConfiguration
                {
                    ManagedServiceId = this.GenerateId(),
                    DescriptiveName = $"({this.GetDescriptiveName()}) {app.ResourceGroupName} / {app.Name}",
                    Configuration = new AzureFunctionServiceConfiguration
                    {
                        TenantId = Configuration.TenantId,
                        SubscriptionId = Configuration.SubscriptionId,
                        ResourceGroup = app.ResourceGroupName,
                        FunctionAppName = app.Name
                    }
                };
                c.LinksInAs.Add($"{app.ResourceGroupName}/{app.Name}");
                c.LinksInAs.AddRange(app.HostNames);

                foreach (var cstr in connstr)
                    c.ConfigurationValues["C|" + cstr.Key] = cstr.Value.Value;
                foreach (var setting in settings)
                    c.ConfigurationValues["A|" + setting.Key] = setting.Value.Value;

                return c;
            }))).ToList();
        }
    }
}