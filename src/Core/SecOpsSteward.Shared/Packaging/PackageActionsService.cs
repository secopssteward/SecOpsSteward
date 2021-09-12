using SecOpsSteward.Plugins;
using SecOpsSteward.Plugins.Configurable;
using SecOpsSteward.Plugins.Discovery;
using SecOpsSteward.Shared.Cryptography;
using SecOpsSteward.Shared.Packaging.Wrappers;
using SecOpsSteward.Shared.Roles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SecOpsSteward.Shared.Packaging
{
    public class PackageActionsService
    {
        private readonly ChimeraServiceConfigurator _configurator;
        private readonly IServiceProvider _services;
        private readonly ICryptographicService _cryptoService;
        private readonly IPackageRepository _packageRepo;
        private readonly IRoleAssignmentService _roleAssignmentService;
        public PackageActionsService(
            ChimeraServiceConfigurator configurator,
            IServiceProvider services,
            ICryptographicService cryptoService,
            IPackageRepository packageRepo,
            IRoleAssignmentService roleAssignmentService)
        {
            _configurator = configurator;
            _services = services;
            _cryptoService = cryptoService;
            _packageRepo = packageRepo;
            _roleAssignmentService = roleAssignmentService;
        }

        /// <summary>
        /// Optional. When specified, packages will be checked against this hash before loading.
        /// </summary>
        public byte[] PublicRepositoryHash { get; set; }

        /// <summary>
        /// Optional. When true, all packages will have their signature chains verified. This increases load time.
        /// </summary>
        public bool ForceSecureLoad { get; set; }

        private List<ChimeraContainer> _containers = new List<ChimeraContainer>();
        private List<ChimeraPackageIdentifier> _securelyLoaded = new List<ChimeraPackageIdentifier>();

        // ---

        public Task<bool> HasAccess(ChimeraPackageIdentifier pluginId, ConfigurableObjectParameterCollection configuration, string identity) =>
            HasAccess(pluginId, configuration.AsSerializedString(), identity);

        public async Task<bool> HasAccess(ChimeraPackageIdentifier pluginId, string configuration, string identity)
        {
            var plugin = await GetPlugin(pluginId, configuration);
            return await _roleAssignmentService.HasAssignedPluginRole(plugin, plugin.RbacRequirements, identity);
        }

        // -----

        public Task Grant(ChimeraPackageIdentifier pluginId, ConfigurableObjectParameterCollection configuration, string identity) =>
            Grant(pluginId, configuration.AsSerializedString(), identity);

        public async Task Grant(ChimeraPackageIdentifier pluginId, string configuration, string identity)
        {
            var plugin = await GetPlugin(pluginId, configuration);
            await _roleAssignmentService.AssignPluginRole(plugin, plugin.RbacRequirements, identity);
        }

        // -----

        public Task Revoke(ChimeraPackageIdentifier pluginId, ConfigurableObjectParameterCollection configuration, string identity) =>
            Revoke(pluginId, configuration.AsSerializedString(), identity);

        public async Task Revoke(ChimeraPackageIdentifier pluginId, string configuration, string identity)
        {
            var plugin = await GetPlugin(pluginId, configuration);
            await _roleAssignmentService.UnassignPluginRole(plugin, plugin.RbacRequirements, identity);
        }

        // -----

        public Task<List<DiscoveredServiceConfiguration>> Discover(ChimeraPackageIdentifier serviceId, ConfigurableObjectParameterCollection configuration) =>
            Discover(serviceId, configuration.AsSerializedString());

        public async Task<List<DiscoveredServiceConfiguration>> Discover(ChimeraPackageIdentifier serviceId, string configuration)
        {
            var svc = await GetService(serviceId, configuration);
            var discovered = await svc.Discover();
            foreach (var d in discovered) d.EntityService = svc;
            return discovered;
        }

        // -

        public Task<List<DiscoveredServiceConfiguration>> Discover(ChimeraPackageIdentifier serviceId, ConfigurableObjectParameterCollection configuration, List<DiscoveredServiceConfiguration> discoveredElements, bool includeSecure = false) =>
            Discover(serviceId, configuration.AsSerializedString(), discoveredElements, includeSecure);

        public async Task<List<DiscoveredServiceConfiguration>> Discover(ChimeraPackageIdentifier serviceId, string configuration, List<DiscoveredServiceConfiguration> discoveredElements, bool includeSecure = false)
        {
            var svc = await GetService(serviceId, configuration);
            var discovered = await svc.Discover(discoveredElements, includeSecure);
            foreach (var d in discovered) d.EntityService = svc;
            return discovered;
        }

        // -----

        public Task<PluginOutputStructure> Execute(ChimeraPackageIdentifier pluginId, ConfigurableObjectParameterCollection configuration, PluginOutputStructure previousOutput) =>
            Execute(pluginId, configuration.AsSerializedString(), previousOutput);

        public async Task<PluginOutputStructure> Execute(ChimeraPackageIdentifier pluginId, string configuration, PluginOutputStructure previousOutput) =>
            await (await GetPlugin(pluginId, configuration)).Execute(previousOutput);

        // -----

        public bool CheckContainerHash(ChimeraPackageIdentifier pluginId, byte[] expectedHash)
        {
            var requestedContainer = new ChimeraPackageIdentifier(pluginId.GetComponents(PackageIdentifierComponents.Container));

            var container = _containers.FirstOrDefault(c => c.GetMetadata().ContainerId == requestedContainer);
            if (container == null) return false;

            else return container.GetMetadata().PackageContentHash.SequenceEqual(expectedHash);
        }

        public async Task<ChimeraContainer> LoadContainer(ChimeraPackageIdentifier pluginId, bool secureLoad = false)
        {
            ChimeraContainer c;
            if (secureLoad || ForceSecureLoad)
                c = await LoadContainerSecurely(pluginId);
            else
                c = await LoadContainerNormally(pluginId);
            return c;
        }

        // -----

        private bool IsContainerLoaded(ChimeraPackageIdentifier packageId)
        {
            lock (_containers) { return _containers.Any(c => c.GetMetadata().ContainerId == packageId); }
        }

        private bool IsContainerLoadedSecurely(ChimeraPackageIdentifier packageId)
        {
            lock (_securelyLoaded) return _securelyLoaded.Contains(packageId);
        }

        private async Task<ChimeraContainer> LoadContainerNormally(ChimeraPackageIdentifier pluginId)
        {
            var requestedContainer = new ChimeraPackageIdentifier(pluginId.GetComponents(PackageIdentifierComponents.Container));

            if (!IsContainerLoaded(requestedContainer))
            {
                var loadedContainer = await AcquireContainer(requestedContainer);
                lock (_containers)
                    _containers.Add(loadedContainer);
            }
            lock (_containers)
                return _containers.First(c => c.GetMetadata().ContainerId == requestedContainer);
        }

        private async Task<ChimeraContainer> LoadContainerSecurely(ChimeraPackageIdentifier pluginId)
        {
            var requestedContainer = new ChimeraPackageIdentifier(pluginId.GetComponents(PackageIdentifierComponents.Container));

            ChimeraContainer container = null;
            bool isLoaded, isLoadedSecurely = false;
            lock (_containers)
                isLoaded = _containers.Any(c => c.GetMetadata().ContainerId == requestedContainer);
            lock (_securelyLoaded)
                isLoadedSecurely = _securelyLoaded.Contains(requestedContainer);

            if (isLoaded) container = await GetContainerFromRepository(requestedContainer);

            if (!IsContainerLoaded(requestedContainer) || !IsContainerLoadedSecurely(requestedContainer))
            {
                if (container != null)
                {
                    container.Dispose();
                    lock (_containers)
                        _containers.Remove(container);
                }

                container = await AcquireContainer(requestedContainer);

                var verification = await container.GetMetadata().Verify(_cryptoService);
                var failures = verification.Where(v => v.Value == false);
                if (failures.Any())
                {
                    container.Dispose();
                    container = null;
                    throw new BadImageFormatException("Package signature verification failed! ({0})",
                        string.Join(", ", failures.Select(f => f.Key.Signer)));
                }

                lock (_containers)
                    _containers.Add(container);

                _securelyLoaded.Add(requestedContainer);
                _securelyLoaded = _securelyLoaded.Distinct().ToList();
            }

            lock (_containers)
                return _containers.First(c => c.GetMetadata().ContainerId == requestedContainer);
        }

        /// <summary>
        /// Acquires container from repository and does basic integrity checks.
        /// * Ensures the content hash matches the metadata hash
        /// * Checks the public signature against the given one (if present)
        /// </summary>
        /// <param name="pluginId">Container to acquire</param>
        /// <returns>Container, when the conditions are met</returns>
        private async Task<ChimeraContainer> AcquireContainer(ChimeraPackageIdentifier pluginId)
        {
            // retrieve
            var container = await GetContainerFromRepository(pluginId);
            var metadata = container.GetMetadata();

            // make sure package content hash matches its metadata hash
            if (!container.GetPackageContentHash().SequenceEqual(metadata.PackageContentHash))
            {
                container.Dispose();
                container = null;
                throw new BadImageFormatException("Package metadata hash and actual content hash differ");
            }

            if (PublicRepositoryHash != null && metadata.PublicSignature != null)
            {
                if (!metadata.PubliclyVerify(PublicRepositoryHash))
                {
                    container.Dispose();
                    container = null;
                    throw new BadImageFormatException("Container's public repository hash does not match the given one");
                }
            }

            return container;
        }

        // -----

        private async Task<ContainerPluginWrapper> GetPluginWrapper(ChimeraPackageIdentifier pluginId) => (await LoadContainer(pluginId)).Wrapper.GetPlugin(pluginId.Id);
        private async Task<IPlugin> GetPlugin(ChimeraPackageIdentifier pluginId, string configuration) => (await GetPluginWrapper(pluginId)).Emit(_services, configuration);

        private async Task<ContainerServiceWrapper> GetServiceWrapper(ChimeraPackageIdentifier serviceId) => (await LoadContainer(serviceId)).Wrapper.GetService(serviceId.Id);
        private async Task<IManagedServicePackage> GetService(ChimeraPackageIdentifier serviceId, string configuration) => (await GetServiceWrapper(serviceId)).Emit(_services, configuration);

        private async Task<ChimeraContainer> GetContainerFromRepository(ChimeraPackageIdentifier packageId)
        {
            return await _packageRepo.Get(packageId);
        }
    }
}
