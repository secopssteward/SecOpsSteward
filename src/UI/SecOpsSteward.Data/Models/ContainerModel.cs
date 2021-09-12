using SecOpsSteward.Shared.Packaging.Metadata;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace SecOpsSteward.Data.Models
{
    public class ContainerModel
    {
        [Key]
        public Guid ContainerId { get; set; }

        public string Version { get; set; }
        public DateTimeOffset InstalledOn { get; set; }

        public ContainerModel() { }
        public static ContainerModel FromMetadata(ContainerMetadata metadata) =>
            new ContainerModel()
            {
                ContainerId = metadata.ContainerId.Id,
                Version = metadata.Version,
                InstalledOn = DateTimeOffset.UtcNow,
                ManagedServices = metadata.ServicesMetadata.Select(svc => ManagedServiceModel.FromMetadata(svc, metadata.PluginsMetadata.ToArray())).ToList()
            };

        public ICollection<ManagedServiceModel> ManagedServices { get; set; } = new List<ManagedServiceModel>();
    }
}
