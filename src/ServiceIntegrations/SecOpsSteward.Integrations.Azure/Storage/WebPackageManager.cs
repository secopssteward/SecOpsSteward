using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Extensions.Logging;
using SecOpsSteward.Plugins.Azure;
using SecOpsSteward.Shared;
using SecOpsSteward.Shared.Packaging;
using SecOpsSteward.Shared.Packaging.Metadata;
using SecOpsSteward.Shared.Roles;
using SecOpsSteward.Shared.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SecOpsSteward.Integrations.Azure.Storage
{
    public class WebPackageManager : AzurePlatformIntegrationBase, IPackageRepository, IHasAgentCreationActions
    {
        private static string GetPackageFileName(ChimeraPackageIdentifier packageId) => $"{PACKAGE_FILE_PREFIX}{packageId.GetComponents(PackageIdentifierComponents.Container)}.zip";
        private static string GetPackageMetadataFileName(ChimeraPackageIdentifier packageId) => $"{PACKAGE_FILE_PREFIX}{packageId.Id}.meta";
        private const string PACKAGE_FILE_PREFIX = "pkg-";

        public bool UseAsUnderlyingApplication = false;

        private string CfgPackageRepoAccount => _configurator["PackageRepoAccount"];
        private string CfgPackageRepoContainer => _configurator["PackageRepoContainer"];

        public int ServicePriority => 50;

        public WebPackageManager(
            ILogger<WebPackageManager> logger,
            ChimeraServiceConfigurator configurator,
            IRoleAssignmentService roleAssignment,
            AzureCurrentCredentialFactory platformFactory) : base(logger, configurator, roleAssignment, platformFactory) { }

        public async Task CreateOrUpdate(ChimeraContainer package)
        {
            var metadata = package.GetMetadata();
            var blob = GetAgentPackageBlob(metadata.ContainerId);

            package.ContainerStream.Seek(0, SeekOrigin.Begin);
            await blob.UploadAsync(package.ContainerStream);

            var metadataEncoded = new MemoryStream(ChimeraSharedHelpers.SerializeToBytes(metadata));
            metadataEncoded.Seek(0, SeekOrigin.Begin);
            var mdBlob = GetAgentPackageMetadataBlob(metadata.ContainerId);
            await mdBlob.UploadAsync(metadataEncoded);
        }

        public async Task Delete(ChimeraPackageIdentifier package)
        {
            var blob = GetAgentPackageBlob(package);
            await blob.DeleteIfExistsAsync();
        }

        public async Task<ChimeraContainer> Get(ChimeraPackageIdentifier package)
        {
            var packageStream = new MemoryStream();
            var blob = GetAgentPackageBlob(package);
            await blob.DownloadToAsync(packageStream);
            packageStream.Seek(0, SeekOrigin.Begin);

            return new ChimeraContainer(packageStream);
        }

        public async Task<ContainerMetadata> GetMetadata(ChimeraPackageIdentifier package)
        {
            var fileName = GetPackageMetadataFileName(package);
            return await GetMetadata(fileName);
        }
        private async Task<ContainerMetadata> GetMetadata(string fileName)
        {
            var ms = new MemoryStream();
            await GetClient().GetBlockBlobClient(fileName).DownloadToAsync(ms);

            ms.Seek(0, SeekOrigin.Begin);

            return ChimeraSharedHelpers.GetFromSerializedBytes<ContainerMetadata>(ms.ToArray());
        }

        public async Task<List<ContainerMetadata>> List()
        {
            var blobs = GetClient()
                .GetBlobs()
                .AsPages()
                .SelectMany(p => p.Values
                    .Where(v => !v.Deleted)
                    .Where(v => v.Name.EndsWith(".meta"))
                    .ToList());

            var results = await Task.WhenAll(blobs.Select(async b => await GetMetadata(b.Name)));
            return results.ToList();
        }

        private BlockBlobClient GetAgentPackageBlob(ChimeraPackageIdentifier agentPackageId)
        {
            var blobCli = GetClient().GetBlockBlobClient(GetPackageFileName(agentPackageId));
            return blobCli;
        }

        private BlockBlobClient GetAgentPackageMetadataBlob(ChimeraPackageIdentifier agentPackageId)
        {
            var blobCli = GetClient().GetBlockBlobClient(GetPackageMetadataFileName(agentPackageId));
            return blobCli;
        }

        private BlobContainerClient GetClient() =>
            new BlobContainerClient(new Uri(
                $"https://{CfgPackageRepoAccount}.blob.core.windows.net/{CfgPackageRepoContainer}"),
                UseAsUnderlyingApplication ?
                    _platformFactory.GetCredentialPreferringAppIdentity().Credential :
                    _platformFactory.GetCredential().Credential
                );

        // ---

        public async Task OnAgentCreated(ChimeraAgentIdentifier agent)
        {
            var scope = BaseScope + "/providers/Microsoft.Storage/storageAccounts/" +
                CfgPackageRepoAccount + "/blobServices/default/containers/" + CfgPackageRepoContainer;

            await _roleAssignment.ApplyScopedRoleToIdentity(agent, AssignableRole.CanReadPackages, scope);
        }

        public Task OnAgentRemoved(ChimeraAgentIdentifier agent) => Task.CompletedTask;
    }
}
