using System.Collections.Generic;
using System.Threading.Tasks;
using SecOpsSteward.Shared.Packaging.Metadata;

namespace SecOpsSteward.Shared.Packaging
{
    /// <summary>
    ///     Handles the transport and storage of Packages
    /// </summary>
    public interface IPackageRepository
    {
        /// <summary>
        ///     Retrieve package metadata
        /// </summary>
        /// <param name="package">Package ID</param>
        /// <returns>List of packages and metadata</returns>
        Task<ContainerMetadata> GetMetadata(ChimeraPackageIdentifier package);

        /// <summary>
        ///     Directly download a package from the repository by its ID
        /// </summary>
        /// <param name="package">Package ID</param>
        /// <returns>Chimera package</returns>
        Task<ChimeraContainer> Get(ChimeraPackageIdentifier package);

        /// <summary>
        ///     List all available packages and their metadata
        /// </summary>
        /// <returns>List of packages and metadata</returns>
        Task<List<ContainerMetadata>> List();

        /// <summary>
        ///     Delete a package from the repository
        /// </summary>
        /// <param name="package">Package ID</param>
        /// <returns></returns>
        Task Delete(ChimeraPackageIdentifier package);

        /// <summary>
        ///     Create or update a package in the repository
        /// </summary>
        /// <param name="package">Package</param>
        /// <returns></returns>
        Task CreateOrUpdate(ChimeraContainer package);
    }
}