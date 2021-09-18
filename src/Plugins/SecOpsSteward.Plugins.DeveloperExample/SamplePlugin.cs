using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using SecOpsSteward.Plugins.Azure;

namespace SecOpsSteward.Plugins.DeveloperExample
{
    public class SamplePluginConfiguration : SampleServiceConfiguration
    {
        // This inherits from the service configuration; it's expected that the plugins
        // are operating on that discovered service, so it becomes part of the config.

        [Required]
        [DisplayName("Enter a color")]
        public string Color { get; set; }

        [Required]
        [DisplayName("Enter your pet's name")]
        public string PetName { get; set; }
    }

    [ElementDescription(
        "Do Some Stuff",
        "Jane Doe",
        "Provides sample plugin code for a developer",
        "1.0.0")]
    [ManagedService(typeof(SampleService))]
    [PossibleResultCodes(CommonResultCodes.Success, CommonResultCodes.Failure)]
    [GeneratedSharedOutputs("Sample/{{$Configuration.Location}}/{{$Configuration.Color}}")]
    public class SamplePlugin : SOSPlugin<SamplePluginConfiguration>
    {
        public override IEnumerable<PluginRbacRequirements> RbacRequirements => new List<PluginRbacRequirements>
        {
            // You can identify the RBAC requirements to run this plugin from the Azure Docs.
            // It is strongly encouraged that you make these permissions as narrow as possible.
            // For example, if writing configuration does not require the "read" permission, do not include "read".
            AzurePluginRbacRequirements.WithActions(
                "Allow the plugin to do stuff",
                "/plugin/scope",
                "Plugin.DoStuff", "Plugin.DoOtherStuff")

            // If there are more scopes, add an item for each here.
        };

        public override async Task<PluginOutputStructure> Execute(PluginOutputStructure previousOutput)
        {
            // Here we perform the unit of work associated with this plugin.

            // "previousOutput" is the result of previous plugins run before this, which provide shared information.
            if (previousOutput.SharedOutputs.Contains("/previous/plugin/output"))
            {
                var lastOutput = previousOutput["/previous/plugin/output"];
            }

            await Task.Yield();

            // If this generates an output, such as a generated password, add it to the PluginOutputStructure.
            // Using .WithSecureOutput will prevent the value from being exposed to the user.
            // Otherwise, using .WithOutput will allow the user to read the value.
            return new PluginOutputStructure(CommonResultCodes.Success)
                .WithSecureOutput($"/Sample/{Configuration.Location}/{Configuration.Color}", "This is a result");
        }
    }
}