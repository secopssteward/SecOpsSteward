using System;
using System.Collections.Generic;
using System.Linq;
using SecOpsSteward.Plugins;
using SecOpsSteward.Plugins.Configurable;
using SecOpsSteward.Plugins.WorkflowTemplates;

namespace SecOpsSteward.Shared.Packaging.Metadata
{
    public class ServiceMetadata
    {
        /// <summary>
        ///     Information stored to describe a Service
        /// </summary>
        /// <param name="service">IManagedServicePackage to extract information from</param>
        public ServiceMetadata(IManagedServicePackage service)
        {
            ServiceId = new ChimeraPackageIdentifier(service.GenerateId());
            Name = service.GetDescriptiveName();
            Description = service.GetDescriptiveDescription();
            ParameterCollection = service.GetConfigurationDescription();
            var pluginTypes = service.GetType().Assembly.GetTypes()
                .Where(t => t.GetInterfaces().Contains(typeof(IPlugin)));
            PluginIds = new List<ChimeraPackageIdentifier>(pluginTypes.Select(p =>
                new ChimeraPackageIdentifier(p.GenerateId())));
            Templates = service.Templates;
        }

        public ServiceMetadata()
        {
        }

        /// <summary>
        ///     Service identifier
        /// </summary>
        public ChimeraPackageIdentifier ServiceId { get; set; }

        /// <summary>
        ///     Service display name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     Description of what the Service does
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        ///     Parameters used to configure the Service
        /// </summary>
        public ConfigurableObjectParameterCollection ParameterCollection { get; set; } = new();

        /// <summary>
        ///     Plugin IDs supported by this Service
        /// </summary>
        public List<ChimeraPackageIdentifier> PluginIds { get; set; }

        /// <summary>
        ///     Templates which this service users
        /// </summary>
        public List<WorkflowTemplateDefinition> Templates { get; set; }

        public void CheckIntegrity()
        {
            if (ServiceId.Id == Guid.Empty) throw new Exception("Service ID Empty");
            if (string.IsNullOrEmpty(Name)) throw new Exception($"[{ServiceId}] Service name not present");
            if (PluginIds.Count == 0) throw new Exception($"[{ServiceId}] No plugins associated with service");

            if (PluginIds.Any(p => p.ServiceId != ServiceId.ServiceId))
                throw new Exception($"[{ServiceId}] A Plugin ID does not match its Service ID");
        }

        public override string ToString()
        {
            return $"Service ID {ServiceId} ({Name}) with {ParameterCollection.Parameters.Count} inputs";
        }
    }
}