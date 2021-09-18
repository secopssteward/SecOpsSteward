using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.Azure.Management.AppService.Fluent.Models;

namespace SecOpsSteward.Plugins.Azure.AppServices.Functions
{
    public class AzureFunctionSlotSwapConfiguration : AzureFunctionServiceConfiguration
    {
        [Required]
        [DisplayName("Source Slot")]
        [Description("Slot to swap from")]
        public string SourceSlot { get; set; }

        [Required]
        [DisplayName("Destination Slot")]
        [Description("Slot to swap to")]
        public string DestinationSlot { get; set; }
    }

    [ElementDescription(
        "Azure Function Slot Swap",
        "Anthony Turner",
        "Swaps the slot associated with an Azure Functions app",
        "1.0.0")]
    [ManagedService(typeof(AzureFunctionService))]
    [PossibleResultCodes(CommonResultCodes.Success, CommonResultCodes.Failure)]
    public class AzureFunctionSlotSwap : SOSPlugin<AzureFunctionSlotSwapConfiguration>
    {
        public AzureFunctionSlotSwap(AzureCurrentCredentialFactory platformFactory)
        {
            PlatformFactory = platformFactory;
            if (platformFactory == null) throw new Exception("Platform handle not found");
        }

        public AzureFunctionSlotSwap()
        {
        }

        protected AzureCurrentCredentialFactory PlatformFactory { get; set; }

        public override IEnumerable<PluginRbacRequirements> RbacRequirements => new[]
        {
            AzurePluginRbacRequirements.WithActions(
                "Swap Between Slots and/or Production Site",
                Configuration.GetScope(),
                // Required to poll status after call
                "microsoft.web/sites/operations/read",
                "microsoft.web/sites/operationresults/read",

                // Read long-running operation status
                "microsoft.web/sites/slots/operations/read",
                "microsoft.web/sites/slots/operationresults/read",

                // Swap action
                "Microsoft.Web/sites/slotsswap/Action",
                "Microsoft.Web/sites/slots/slotsswap/Action",

                // Read slot config diffs
                "Microsoft.Web/sites/slotsdiffs/Action",
                "Microsoft.Web/sites/slots/slotsdiffs/Action")
        };

        public override async Task<PluginOutputStructure> Execute(PluginOutputStructure previousOutput)
        {
            var azure = PlatformFactory.GetCredential(Configuration.SubscriptionId).GetAzure();

            if (Configuration.SourceSlot.ToLower() == "production" ||
                Configuration.DestinationSlot.ToLower() == "production")
            {
                var otherSlot = Configuration.SourceSlot == "production"
                    ? Configuration.DestinationSlot
                    : Configuration.SourceSlot;

                await azure.AppServices.FunctionApps.Inner.SwapSlotWithProductionWithHttpMessagesAsync(
                    Configuration.ResourceGroup,
                    Configuration.FunctionAppName,
                    new CsmSlotEntity(
                        otherSlot, true));
            }
            else
            {
                await azure.AppServices.FunctionApps.Inner.SwapSlotSlotWithHttpMessagesAsync(
                    Configuration.ResourceGroup,
                    Configuration.FunctionAppName,
                    new CsmSlotEntity(
                        Configuration.SourceSlot, true),
                    Configuration.DestinationSlot);
            }

            // responds with NotFound if the slot doesn't exist

            return new PluginOutputStructure(CommonResultCodes.Success);
        }
    }
}