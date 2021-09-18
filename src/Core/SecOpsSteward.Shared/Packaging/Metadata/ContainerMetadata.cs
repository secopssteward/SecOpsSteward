using System;
using System.Collections.Generic;
using System.Linq;
using SecOpsSteward.Shared.Cryptography;
using SecOpsSteward.Shared.Cryptography.Extensions;

namespace SecOpsSteward.Shared.Packaging.Metadata
{
    public class ContainerMetadata : ISignable, ISignableByMany, IPubliclySignable
    {
        /// <summary>
        ///     Information stored to describe a Container
        /// </summary>
        /// <param name="container">ChimeraContainer to extract information from</param>
        public ContainerMetadata(ChimeraContainer container)
        {
            // get the version from the assembly
            container.Wrapper.Load();
            Version = container.Wrapper.Assembly.GetName().Version.ToString();
            Signatures = new List<ChimeraEntitySignature>();
        }

        public ContainerMetadata()
        {
        }

        /// <summary>
        ///     Container ID (first 8 of guid)
        /// </summary>
        public ChimeraPackageIdentifier ContainerId { get; set; }

        /// <summary>
        ///     Container version
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        ///     Data hash of the package content this metadata represents
        /// </summary>
        public byte[] PackageContentHash { get; set; }

        /// <summary>
        ///     Plugins in the container
        /// </summary>
        public List<PluginMetadata> PluginsMetadata { get; set; } = new();

        /// <summary>
        ///     Services in the container
        /// </summary>
        public List<ServiceMetadata> ServicesMetadata { get; set; } = new();

        /// <summary>
        ///     A signature from a public entity outside the scope of the tenant
        /// </summary>
        public PublicSignature PublicSignature { get; set; }

        /// <summary>
        ///     Signatures of Users who have attested to the intent, scope, and integrity of the Package
        /// </summary>
        public List<ChimeraEntitySignature> Signatures { get; set; } = new();

        public void CheckIntegrity()
        {
            if (ContainerId.Id == Guid.Empty) throw new Exception("Container ID Empty");

            if (ServicesMetadata.Any(s => s.ServiceId.ContainerId != ContainerId.ContainerId))
                throw new Exception("A Service ID does not match Container ID");
            if (PluginsMetadata.Any(s => s.PluginId.ContainerId != ContainerId.ContainerId))
                throw new Exception("A Plugin ID does not match Container ID");

            if (!PackageContentHash.Any(b => b != 0x00))
                throw new Exception("Plugin content hash was not generated");

            PluginsMetadata.ForEach(p => p.CheckIntegrity());
            ServicesMetadata.ForEach(p => p.CheckIntegrity());
        }

        public override string ToString()
        {
            return $"Container with {PluginsMetadata.Count} plugins and {ServicesMetadata.Count} services";
        }
    }
}