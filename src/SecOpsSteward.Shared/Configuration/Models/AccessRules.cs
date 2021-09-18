using System.Collections.Generic;
using System.Linq;

namespace SecOpsSteward.Shared.Configuration.Models
{
    /// <summary>
    ///     Rules for which users can run which packages on an Agent
    /// </summary>
    public partial class AccessRules
    {
        private List<AccessRule> Items { get; set; } = new();

        /// <summary>
        ///     Add access for a User to run a Package
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="packageId">Package ID</param>
        public void Add(ChimeraUserIdentifier userId, ChimeraPackageIdentifier packageId)
        {
            Items.Add(new AccessRule
            {
                UserId = userId,
                PackageId = packageId
            });
            Items = Items.Distinct().ToList();
        }

        /// <summary>
        ///     Remove access from a User and Package
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="packageId">Package ID</param>
        public void Remove(ChimeraUserIdentifier userId, ChimeraPackageIdentifier packageId)
        {
            var item = Items.FirstOrDefault(i => i.UserId == userId && i.PackageId == packageId);
            if (item != null)
                Items.Remove(item);
        }

        /// <summary>
        ///     Check if a given User and Package combination have access
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="packageId">Package ID</param>
        /// <returns></returns>
        public bool HasAccess(ChimeraUserIdentifier userId, ChimeraPackageIdentifier packageId)
        {
            return Items.Any(i => i.UserId == userId && i.PackageId == packageId);
        }
    }
}