using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace SecOpsSteward.Plugins.Azure.AppServices.Functions
{
    public class SetFunctionConfigurationConfiguration : AzureFunctionServiceConfiguration
    {
        [Required]
        [DisplayName("Configuration Item Name")]
        public string Name { get; set; }

        [Required]
        [DisplayName("Configuration Item Value")]
        public string Value { get; set; }
    }

    [ElementDescription(
        "Set Function Configuration",
        "Anthony Turner",
        "Sets a configuration value for an Azure Function",
        "1.0.0")]
    [ManagedService(typeof(AzureFunctionService))]
    [PossibleResultCodes(CommonResultCodes.Success, CommonResultCodes.Failure)]
    [RequiredSharedInputs("&&/Name/Value")]
    public class SetFunctionConfiguration : SOSPlugin<SetFunctionConfigurationConfiguration>
    {
        public SetFunctionConfiguration(AzureCurrentCredentialFactory platformFactory)
        {
            PlatformFactory = platformFactory;
            if (platformFactory == null) throw new Exception("Platform handle not found");
        }

        public SetFunctionConfiguration()
        {
        }

        protected AzureCurrentCredentialFactory PlatformFactory { get; set; }

        public override IEnumerable<PluginRbacRequirements> RbacRequirements => new[]
        {
            AzurePluginRbacRequirements.WithActions(
                "Read, Write, and List Configurations, Read/Write Site",
                Configuration.GetScope(),
                "Microsoft.Web/sites/config/Read",
                "Microsoft.Web/sites/config/Write",
                "Microsoft.Web/sites/config/list/action",
                "Microsoft.Web/sites/Read",
                "Microsoft.Web/sites/Write")
        };

        public override async Task<PluginOutputStructure> Execute(PluginOutputStructure previousOutput)
        {
            var azure = PlatformFactory.GetCredential(Configuration.SubscriptionId).GetAzure();
            var app = await Configuration.GetAppAsync(azure);

            // apply inputs to configured

            var value = TemplatedStrings.PopulateInputsInTemplateString(Configuration.Value, previousOutput);

            await app.Update()
                .WithAppSetting(Configuration.Name, value)
                .ApplyAsync();

            return new PluginOutputStructure(CommonResultCodes.Success);
        }
    }
}