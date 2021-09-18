using System;
using System.Collections.Generic;
using System.Text.Json;
using SecOpsSteward.Plugins.Discovery;

namespace SecOpsSteward.Plugins.WorkflowTemplates
{
    public class WorkflowTemplateParticipantDefinition
    {
        public WorkflowTemplateParticipantDefinition()
        {
        }

        public WorkflowTemplateParticipantDefinition(Dictionary<string, string> mappings)
        {
            ConfigurationMappings = mappings;
        }

        public WorkflowTemplateParticipantDefinition(Guid packageId, Dictionary<string, string> mappings) :
            this(mappings)
        {
            PackageId = packageId;
        }

        /// <summary>
        ///     Display name for workflow step
        /// </summary>
        public string WorkflowStepName { get; set; }

        /// <summary>
        ///     Package ID which is executed in this step
        /// </summary>
        public Guid PackageId { get; set; } = Guid.Empty;

        /// <summary>
        ///     Package type
        /// </summary>
        public string PackageType { get; set; }

        /// <summary>
        ///     Configuration assigned to this participant definition when built into path sets
        /// </summary>
        public DiscoveredServiceConfiguration ServiceConfiguration { get; set; }

        /// <summary>
        ///     Configuration mappings from the templated workflow to a plugin's configuration
        /// </summary>
        public Dictionary<string, string> ConfigurationMappings
        {
            get => JsonSerializer.Deserialize<Dictionary<string, string>>(MappingJson);
            set => MappingJson = JsonSerializer.Serialize(value);
        }

        public string MappingJson { get; set; } = "{}";

        public static WorkflowTemplateParticipantDefinition DependentFlowParticipant =>
            new();

        public override string ToString()
        {
            return $"Workflow Step '{WorkflowStepName}' ({PackageId})";
        }
    }

    public class WorkflowTemplateParticipantDefinition<T> : WorkflowTemplateParticipantDefinition
        where T : IPlugin, new()
    {
        public WorkflowTemplateParticipantDefinition()
        {
            PackageId = new T().GenerateId();
            WorkflowStepName = new T().GetDescriptiveName();
        }

        public WorkflowTemplateParticipantDefinition(Dictionary<string, string> mappings) : base(mappings)
        {
            PackageId = new T().GenerateId();
            WorkflowStepName = new T().GetDescriptiveName();
        }
    }
}