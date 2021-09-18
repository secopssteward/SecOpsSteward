using System;
using System.Collections.Generic;
using System.Linq;

namespace SecOpsSteward.Shared.Configuration.Models
{
    public partial class AgentPrivilegeGrants
    {
        /// <summary>
        ///     Collection of grants performed for an Agent
        /// </summary>
        private List<AgentPrivilegeGrant> Items { get; } = new();

        /// <summary>
        ///     Add a tracking record for a newly performed grant
        /// </summary>
        /// <param name="granterId">User performing grant</param>
        /// <param name="packageId">Package requiring a privilege granted</param>
        /// <param name="requirement">Access grant requirement (for deduplication)</param>
        public void Add(ChimeraUserIdentifier granterId, ChimeraPackageIdentifier packageId, string requirement)
        {
            var existing =
                Items.FirstOrDefault(i => i.PackageIds.Contains(packageId) && i.AccessRequirement == requirement);
            if (existing != null)
                existing.PackageIds.Add(packageId);
            else
                Items.Add(new AgentPrivilegeGrant
                {
                    AccessRequirement = requirement,
                    GranterId = granterId,
                    Timestamp = DateTimeOffset.UtcNow
                });
        }

        /// <summary>
        ///     Remove the tracking record for a previously granted privilege when revocation was performed
        /// </summary>
        /// <param name="packageId">Package ID which was revoked</param>
        public void Remove(ChimeraPackageIdentifier packageId)
        {
            var existing = Items.Where(i => i.PackageIds.Contains(packageId)).ToList();
            foreach (var item in existing)
            {
                item.PackageIds.Remove(packageId);
                if (!item.PackageIds.Any())
                    Items.Remove(item);
            }
        }

        /// <summary>
        ///     Remove a specific single requirement from the tracked grants for a Package
        /// </summary>
        /// <param name="packageId">Package ID</param>
        /// <param name="requirement">Access requirement description (for deduplication)</param>
        public void Remove(ChimeraPackageIdentifier packageId, string requirement)
        {
            var existing =
                Items.FirstOrDefault(i => i.PackageIds.Contains(packageId) && i.AccessRequirement == requirement);

            if (existing == null) return;

            existing.PackageIds.Remove(packageId);
            if (!existing.PackageIds.Any())
                Items.Remove(existing);
        }
    }
}