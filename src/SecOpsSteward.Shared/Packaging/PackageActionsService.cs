using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SecOpsSteward.Plugins;
using SecOpsSteward.Plugins.Configurable;
using SecOpsSteward.Plugins.Discovery;
using SecOpsSteward.Shared.Cryptography.Extensions;
using SecOpsSteward.Shared.Packaging.Wrappers;
using SecOpsSteward.Shared.Roles;

namespace SecOpsSteward.Shared.Packaging
{
    public class PackageActionsService
    {
        private readonly ICryptographicService _cryptoService;
        private readonly IPackageRepository _packageRepo;
        private readonly IRoleAssignmentService _roleAssignmentService;
        private readonly IServiceProvider _services;

        private readonly List<ChimeraContainer> _containers = new();
        private List<ChimeraPackageIdentifier> _securelyLoaded = new();

        public PackageActionsService(
            IServiceProvider services,
            ICryptographicService cryptoService,
            IPackageRepository packageRepo,
            IRoleAssignmentService roleAssignmentService)
        {
            _services = services;
            _cryptoService = cryptoService;
            _packageRepo = packageRepo;
            _roleAssignmentService = roleAssignmentService;
        }

        /// <summary>
        ///     Optional. When specified, packages will be checked against this hash before loading.
        /// </summary>
        public byte[] PublicRepositoryHash { get; set; }

        /// <summary>
        ///     Optional. When true, all packages will have their signature chains verified. This increases load time.
        /// </summary>
        public bool ForceSecureLoad { get; set; }


        /// <summary>
        ///     If the Identity has the proper rights to perform the operation in the Plugin, with the given Configuration
        /// </summary>
        /// <param name="pluginId">Plugin ID to test</param>
        /// <param name="configuration">Configuration for Plugin</param>
        /// <param name="identity">Identity to test</param>
        /// <returns><c>TRUE</c> if the identity has the rights required to perform the operation in the Plugin</returns>
        public async Task<bool> HasAccess(ChimeraPackageIdentifier pluginId,
            ConfigurableObjectParameterCollection configuration, string identity)
        {
            var plugin = await GetPlugin(pluginId, configuration.AsSerializedString());
            return await _roleAssignmentService.HasAssignedPluginRole(plugin, plugin.RbacRequirements, identity);
        }


        /// <summary>
        ///     Get the requirements to present to the user for what the Plugin requires to execute
        /// </summary>
        /// <param name="pluginId">Plugin ID to check</param>
        /// <param name="configuration">Configuration of Plugin ID</param>
        /// <returns>List of RBAC requirements necessary to run the Plugin with this Configuration</returns>
        public async Task<IEnumerable<PluginRbacRequirements>> GetRbacRequirements(ChimeraPackageIdentifier pluginId,
            ConfigurableObjectParameterCollection configuration)
        {
            var plugin = await GetPlugin(pluginId, configuration.AsSerializedString());
            return plugin.RbacRequirements;
        }


        /// <summary>
        ///     Grant access to the given Identity to execute the operation in a Plugin with a Configuration
        /// </summary>
        /// <param name="pluginId">Plugin ID to use for access requirements</param>
        /// <param name="configuration">Configuration for Plugin</param>
        /// <param name="identity">Identity OID to grant</param>
        public async Task Grant(ChimeraPackageIdentifier pluginId, ConfigurableObjectParameterCollection configuration,
            string identity)
        {
            var plugin = await GetPlugin(pluginId, configuration.AsSerializedString());
            await _roleAssignmentService.AssignPluginRole(plugin, plugin.RbacRequirements, identity);
        }


        /// <summary>
        ///     Revoke access from the given Identity which allows execution of the operation in a Plugin with a Configuration
        /// </summary>
        /// <param name="pluginId">Plugin ID to use for access requirements</param>
        /// <param name="configuration">Configuration for Plugin</param>
        /// <param name="identity">Identity OID to revoke</param>
        public async Task Revoke(ChimeraPackageIdentifier pluginId, ConfigurableObjectParameterCollection configuration,
            string identity)
        {
            var plugin = await GetPlugin(pluginId, configuration.AsSerializedString());
            await _roleAssignmentService.UnassignPluginRole(plugin, plugin.RbacRequirements, identity);
        }


        /// <summary>
        ///     Discover any possible Managed Services in a given configuration (such as providing Subscription ID)
        /// </summary>
        /// <param name="serviceId">Service to check for</param>
        /// <param name="configuration">Base configuration to use when checking</param>
        /// <returns>
        ///     List of discovered Managed Services which can be used in the system and the configurations required to use
        ///     them
        /// </returns>
        public async Task<List<DiscoveredServiceConfiguration>> Discover(ChimeraPackageIdentifier serviceId,
            string configuration)
        {
            var svc = await GetService(serviceId, configuration);
            var discovered = await svc.Discover();
            foreach (var d in discovered) d.EntityService = svc;
            return discovered;
        }

        /// <summary>
        ///     Run Phase-2 Discovery of any possible Managed Services in a given configuration (such as providing Subscription ID)
        /// </summary>
        /// <param name="serviceId">Service to check for</param>
        /// <param name="configuration">Base configuration to use when checking</param>
        /// <param name="discoveredElements">Discovered elements from Phase-1 Discovery</param>
        /// <param name="includeSecure">Include secure elements in resource Discovery</param>
        /// <returns>
        ///     List of discovered Managed Services which can be used in the system and the configurations required to use
        ///     them
        /// </returns>
        public async Task<List<DiscoveredServiceConfiguration>> Discover(ChimeraPackageIdentifier serviceId,
            string configuration, List<DiscoveredServiceConfiguration> discoveredElements, bool includeSecure = false)
        {
            var svc = await GetService(serviceId, configuration);
            var discovered = await svc.Discover(discoveredElements, includeSecure);
            foreach (var d in discovered) d.EntityService = svc;
            return discovered;
        }


        /// <summary>
        ///     Execute a Plugin with a given configuration and using a previous output
        /// </summary>
        /// <param name="pluginId">Plugin ID</param>
        /// <param name="configuration">Plugin Configuration</param>
        /// <param name="previousOutput">Output from previous Plugin</param>
        /// <returns>Output for next Plugin</returns>
        public async Task<PluginOutputStructure> Execute(ChimeraPackageIdentifier pluginId,
            ConfigurableObjectParameterCollection configuration, PluginOutputStructure previousOutput)
        {
            return await (await GetPlugin(pluginId, configuration.AsSerializedString())).Execute(previousOutput);
        }


        /// <summary>
        ///     Validate the hash of the Container Package against a known value
        /// </summary>
        /// <param name="containerId">Package Container ID to check</param>
        /// <param name="expectedHash">Expected hash</param>
        /// <returns></returns>
        public bool CheckContainerHash(ChimeraPackageIdentifier containerId, byte[] expectedHash)
        {
            var requestedContainer =
                new ChimeraPackageIdentifier(containerId.GetComponents(PackageIdentifierComponents.Container));

            var container = _containers.FirstOrDefault(c => c.GetMetadata().ContainerId == requestedContainer);
            if (container == null) return false;

            return container.GetMetadata().PackageContentHash.SequenceEqual(expectedHash);
        }


        /// <summary>
        ///     Load a Package Container
        /// </summary>
        /// <param name="pluginId">Package Container ID to load</param>
        /// <param name="secureLoad">If the load should be done securely (with signature verification)</param>
        /// <returns>Loaded Package Container</returns>
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
            lock (_containers)
            {
                return _containers.Any(c => c.GetMetadata().ContainerId == packageId);
            }
        }

        private bool IsContainerLoadedSecurely(ChimeraPackageIdentifier packageId)
        {
            lock (_securelyLoaded)
            {
                return _securelyLoaded.Contains(packageId);
            }
        }

        private async Task<ChimeraContainer> LoadContainerNormally(ChimeraPackageIdentifier pluginId)
        {
            var requestedContainer =
                new ChimeraPackageIdentifier(pluginId.GetComponents(PackageIdentifierComponents.Container));

            if (!IsContainerLoaded(requestedContainer))
            {
                var loadedContainer = await AcquireContainer(requestedContainer);
                lock (_containers)
                {
                    _containers.Add(loadedContainer);
                }
            }

            lock (_containers)
            {
                return _containers.First(c => c.GetMetadata().ContainerId == requestedContainer);
            }
        }

        private async Task<ChimeraContainer> LoadContainerSecurely(ChimeraPackageIdentifier pluginId)
        {
            var requestedContainer =
                new ChimeraPackageIdentifier(pluginId.GetComponents(PackageIdentifierComponents.Container));

            ChimeraContainer container = null;
            bool isLoaded, isLoadedSecurely = false;
            lock (_containers)
            {
                isLoaded = _containers.Any(c => c.GetMetadata().ContainerId == requestedContainer);
            }

            lock (_securelyLoaded)
            {
                isLoadedSecurely = _securelyLoaded.Contains(requestedContainer);
            }

            if (isLoaded) container = await GetContainerFromRepository(requestedContainer);

            if (!IsContainerLoaded(requestedContainer) || !IsContainerLoadedSecurely(requestedContainer))
            {
                if (container != null)
                {
                    container.Dispose();
                    lock (_containers)
                    {
                        _containers.Remove(container);
                    }
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
                {
                    _containers.Add(container);
                }

                _securelyLoaded.Add(requestedContainer);
                _securelyLoaded = _securelyLoaded.Distinct().ToList();
            }

            lock (_containers)
            {
                return _containers.First(c => c.GetMetadata().ContainerId == requestedContainer);
            }
        }

        /// <summary>
        ///     Acquires container from repository and does basic integrity checks.
        ///     * Ensures the content hash matches the metadata hash
        ///     * Checks the public signature against the given one (if present)
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
                if (!metadata.PubliclyVerify(PublicRepositoryHash))
                {
                    container.Dispose();
                    container = null;
                    throw new BadImageFormatException(
                        "Container's public repository hash does not match the given one");
                }

            return container;
        }

        // -----

        private async Task<ContainerPluginWrapper> GetPluginWrapper(ChimeraPackageIdentifier pluginId)
        {
            return (await LoadContainer(pluginId)).Wrapper.GetPlugin(pluginId.Id);
        }

        private async Task<IPlugin> GetPlugin(ChimeraPackageIdentifier pluginId, string configuration)
        {
            return (await GetPluginWrapper(pluginId)).Emit(_services, configuration);
        }

        private async Task<ContainerServiceWrapper> GetServiceWrapper(ChimeraPackageIdentifier serviceId)
        {
            return (await LoadContainer(serviceId)).Wrapper.GetService(serviceId.Id);
        }

        private async Task<IManagedServicePackage> GetService(ChimeraPackageIdentifier serviceId, string configuration)
        {
            return (await GetServiceWrapper(serviceId)).Emit(_services, configuration);
        }

        private async Task<ChimeraContainer> GetContainerFromRepository(ChimeraPackageIdentifier packageId)
        {
            return await _packageRepo.Get(packageId);
        }
    }
}