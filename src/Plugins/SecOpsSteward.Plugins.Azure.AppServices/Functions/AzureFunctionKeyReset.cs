using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using SecOpsSteward.Plugins.Configurable;

namespace SecOpsSteward.Plugins.Azure.AppServices.Functions
{
    public class AzureFunctionKeyResetConfiguration : AzureFunctionServiceConfiguration,
        IConfigurableObjectConfiguration
    {
        [Required]
        [DisplayName("Function Name")]
        [Description("Name of Function whose key will be reset")]
        public string FunctionName { get; set; }

        [Required]
        [DisplayName("Function Key Name")]
        [Description("Name of Function Key to reset")]
        public string FunctionKeyName { get; set; }
    }

    [ElementDescription(
        "Azure Function Key Reset",
        "Anthony Turner",
        "Resets the Function Key associated with an Azure Functions application",
        "1.0.0")]
    [PossibleResultCodes(CommonResultCodes.Success, CommonResultCodes.Failure)]
    [ManagedService(typeof(AzureFunctionService))]
    [GeneratedSharedOutputs(
        "Function/{{$Configuration.FunctionAppName}}/{{$Configuration.FunctionName}}/{{$Configuration.FunctionKeyName}}")]
    public class AzureFunctionKeyReset : SOSPlugin<AzureFunctionKeyResetConfiguration>
    {
        public AzureFunctionKeyReset(AzureCurrentCredentialFactory platformFactory)
        {
            PlatformFactory = platformFactory;
            if (platformFactory == null) throw new Exception("Platform handle not found");
        }

        public AzureFunctionKeyReset()
        {
        }

        protected AzureCurrentCredentialFactory PlatformFactory { get; set; }

        public override IEnumerable<PluginRbacRequirements> RbacRequirements => new[]
        {
            AzurePluginRbacRequirements.WithActions(
                "Write & Delete Function Keys for Site and Slots",
                Configuration.GetScope(),
                "Microsoft.Web/sites/slots/host/functionkeys/write",
                "Microsoft.Web/sites/slots/host/functionkeys/delete",
                "Microsoft.Web/sites/host/functionkeys/write",
                "Microsoft.Web/sites/host/functionkeys/delete")
        };

        public override async Task<PluginOutputStructure> Execute(PluginOutputStructure previousOutput)
        {
            var azure = PlatformFactory.GetCredential(Configuration.SubscriptionId).GetAzure();
            var app = await Configuration.GetAppAsync(azure);

            await app.RemoveFunctionKeyAsync(Configuration.FunctionAppName, Configuration.FunctionKeyName);

            var newKey = PluginSharedHelpers.RandomString(64);
            await app.AddFunctionKeyAsync(Configuration.FunctionAppName, Configuration.FunctionKeyName, newKey);

            return new PluginOutputStructure(CommonResultCodes.Success)
                .WithSecureOutput(
                    $"Function/{Configuration.FunctionAppName}/{Configuration.FunctionName}/{Configuration.FunctionKeyName}",
                    newKey);
        }
    }
}