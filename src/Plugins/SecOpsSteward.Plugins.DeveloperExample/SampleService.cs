using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using SecOpsSteward.Plugins.Configurable;
using SecOpsSteward.Plugins.Discovery;
using SecOpsSteward.Plugins.WorkflowTemplates;

namespace SecOpsSteward.Plugins.DeveloperExample
{
    public class SampleServiceConfiguration : IConfigurableObjectConfiguration
    {
        [Required]
        [DisplayName("Meaning of Life")]
        [Description("What is the meaning of life? Consider everything.")]
        public string MeaningOfLife { get; set; }

        [Required]
        [IdentifiesTargetGrantScope]
        [DisplayName("Location")]
        [Description("Where are we going?")]
        public string Location { get; set; }
    }

    [ElementDescription(
        "Developer Sample Service",
        "Shows developers how to use ManagedServices")]
    public class SampleService : SOSManagedService<SampleServiceConfiguration>
    {
        public override ManagedServiceRole Role => ManagedServiceRole.Hybrid;

        public override List<WorkflowTemplateDefinition> Templates => new()
        {
            // Templates are groups of plugins with a known complex action which can be specified here
            SAMPLE_TEMPLATE
        };

        // ---

        private static WorkflowTemplateDefinition SAMPLE_TEMPLATE =>
            new WorkflowTemplateDefinition<SampleService, SampleTemplateConfiguration>(
                "Run a Couple Plugins",
                new WorkflowTemplateParticipantDefinition<SamplePlugin>(new Dictionary<string, string>
                {
                    // Map from the template configuration's values to the various plugin values. This can be useful
                    // for templates which have multiple plugins which share template config values.
                    {nameof(SampleTemplateConfiguration.MappedValue), nameof(SamplePluginConfiguration.Color)}
                }),

                // If there is something that can happen after this, specify it here.
                // This is generally only needed if the plugin(s) output a value which can be consumed elsewhere.
                // This is only used by the Discovery Wizard sequencer.
                WorkflowTemplateParticipantDefinition.DependentFlowParticipant
            );

        // This is "Phase 1" Discovery. It is run first, and will frequently be the only one required.
        public override Task<List<DiscoveredServiceConfiguration>> Discover()
        {
            var discovered = new List<DiscoveredServiceConfiguration>();
            for (var i = 0; i < 2; i++)
                discovered.Add(new DiscoveredServiceConfiguration
                {
                    // Describes this discovery for the user
                    DescriptiveName = $"Sample Service {i}",

                    // Uniquely identifying this resource among all discovered
                    Identifier = $"/sample/svc/{i}",

                    // Provides the configuration necessary to use this discovered service instance
                    Configuration = new SampleServiceConfiguration
                    {
                        Location = $"At Service {i}"
                    },

                    // A list of ways this service can be reached from elsewhere
                    LinksInAs = new List<string>
                    {
                        $"sampleSvc/{i}"
                    },

                    // A list of services this service has as dependencies
                    // e.g. A web service depends on a storage account
                    LinksOutTo = new List<string>
                    {
                        $"storage/sampleSvc/{i}"
                    },

                    // A dictionary listing all of the configuration values present
                    // on this service. This is for the purpose of identifying additional
                    // links out and allowing the adjustment of configuration values.
                    ConfigurationValues = new Dictionary<string, string>
                    {
                        {"CfgA", "A"},
                        {"CfgB", "B"}
                    }
                });
            return Task.FromResult(discovered);
        }

        // This is "Phase 2" Discovery. It is run after all registered services have finished Phase 1 discovery.
        // This method is given the result of that discovery operation, to help do deeper discovery.
        //
        // For example, if Phase 1 can use an enumeration function to produce a result, and there is another related service which is not enumerable,
        // you can use that Phase 1 result to identify the related service, and return _its_ configuration here.
        //
        // This is not required, so you can return an empty set here if this doesn't apply.
        public override Task<List<DiscoveredServiceConfiguration>> Discover(
            List<DiscoveredServiceConfiguration> existingDiscoveries, bool includeSecureElements = false)
        {
            return Task.FromResult(new List<DiscoveredServiceConfiguration>());
        }
    }

    public class SampleTemplateConfiguration : SampleServiceConfiguration
    {
        [DisplayName]
        [Description("This value is prompted when the template is added, and can map to any plugin values")]
        public string MappedValue { get; set; }
    }
}