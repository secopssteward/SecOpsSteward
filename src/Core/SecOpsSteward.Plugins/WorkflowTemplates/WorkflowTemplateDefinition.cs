using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using SecOpsSteward.Plugins.Configurable;

namespace SecOpsSteward.Plugins.WorkflowTemplates
{
    /// <summary>
    ///     Defines a templated workflow which can be inserted into a user-created workflow
    /// </summary>
    public class WorkflowTemplateDefinition
    {
        /// <summary>
        ///     Fixed workflow template ID
        /// </summary>
        public Guid WorkflowTemplateId { get; set; }

        /// <summary>
        ///     Name of templated workflow
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     Templated workflow Configuration
        /// </summary>
        public ConfigurableObjectParameterCollection Configuration
        {
            get => JsonSerializer.Deserialize<ConfigurableObjectParameterCollection>(ConfigurationJson);
            set => ConfigurationJson = JsonSerializer.Serialize(value);
        }

        public string ConfigurationJson { get; set; }

        /// <summary>
        ///     Participant plugins for this templated workflow and their configuration mappings
        /// </summary>
        public List<WorkflowTemplateParticipantDefinition> Participants { get; set; } = new();

        public WorkflowTemplateDefinition Clone()
        {
            return new WorkflowTemplateDefinition
            {
                WorkflowTemplateId = WorkflowTemplateId,
                Name = Name,
                ConfigurationJson = ConfigurationJson,
                Participants = new List<WorkflowTemplateParticipantDefinition>(Participants)
            };
        }

        public WorkflowTemplateDefinition RunWorkflowStep<TParticipant>(
            params KeyValuePair<string, string>[] mappings) where TParticipant : IPlugin, new()
        {
            Participants.Add(
                new WorkflowTemplateParticipantDefinition<TParticipant>(mappings.ToDictionary(k => k.Key,
                    v => v.Value)));
            return this;
        }

        public WorkflowTemplateDefinition RunAnyChildWorkflows()
        {
            Participants.Add(new WorkflowTemplateParticipantDefinition());
            return this;
        }

        public override string ToString()
        {
            return $"Workflow '{Name}' ({Participants.Count})";
        }
    }

    public class WorkflowTemplateDefinition<TManagedService, TConfiguration> : WorkflowTemplateDefinition
        where TManagedService : IManagedServicePackage
        where TConfiguration : IConfigurableObjectConfiguration, new()
    {
        public WorkflowTemplateDefinition()
        {
            Configuration = ConfigurableObjectParameterCollection.CreateFromObject(new TConfiguration());
        }

        public WorkflowTemplateDefinition(string name) : this()
        {
            Name = name;
            WorkflowTemplateId = IdGenerationExtensions.GenerateWorkflowId<TManagedService>(name);
        }

        public WorkflowTemplateDefinition(string name,
            params WorkflowTemplateParticipantDefinition[] participantDefinitions) : this(name)
        {
            foreach (var item in participantDefinitions)
                Participants.Add(item);
        }
    }

    public static class WorkflowTemplateDefinitionExtensions
    {
        public static KeyValuePair<string, string> MapsTo(this string key, string value)
        {
            return new(key, value);
        }
    }
}