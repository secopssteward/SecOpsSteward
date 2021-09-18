using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SecOpsSteward.Shared.Packaging.Metadata;

namespace SecOpsSteward.Shared.Packaging
{
    public class DummyStorageRepository : IPackageRepository
    {
        public static Dictionary<ChimeraPackageIdentifier, byte[]> _data = new();

        private readonly ILogger<DummyStorageRepository> _logger;

        public DummyStorageRepository(ILogger<DummyStorageRepository> logger)
        {
            _logger = logger;
        }

        public async Task CreateOrUpdate(ChimeraContainer package)
        {
            await Task.Yield();
            _logger.LogTrace($"Create/Update package {package}");
            var ms = new MemoryStream();
            var stream = package.ContainerStream;
            stream.Seek(0, SeekOrigin.Begin);
            stream.CopyTo(ms);
            ms.Seek(0, SeekOrigin.Begin);
            var data = ms.ToArray();

            _data[package.GetMetadata().ContainerId] = data;
        }

        public async Task Delete(ChimeraPackageIdentifier package)
        {
            await Task.Yield();
            _logger.LogTrace($"Delete package {package}");
            _data.Remove(package.GetComponents(PackageIdentifierComponents.Container));
        }

        public async Task<ChimeraContainer> Get(ChimeraPackageIdentifier package)
        {
            await Task.Yield();
            _logger.LogTrace($"Retrieve package {package}");
            var data = _data[package.GetComponents(PackageIdentifierComponents.Container)];
            var ms = new MemoryStream(data);
            ms.Seek(0, SeekOrigin.Begin);
            return new ChimeraContainer(ms);
        }

        public async Task<ContainerMetadata> GetMetadata(ChimeraPackageIdentifier package)
        {
            _logger.LogTrace($"Get metadata for {package}");
            return (await Get(package)).GetMetadata();
        }

        public async Task<List<ContainerMetadata>> List()
        {
            _logger.LogTrace($"List packages ({_data.Count})");
            var results = await Task.WhenAll(_data.Select(async d => (await Get(d.Key)).GetMetadata()));
            return results.ToList();
        }
    }
}