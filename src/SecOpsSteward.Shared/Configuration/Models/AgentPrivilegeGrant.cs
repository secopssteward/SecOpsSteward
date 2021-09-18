using System;
using System.Collections.Generic;

namespace SecOpsSteward.Shared.Configuration.Models
{
    public partial class AgentPrivilegeGrants
    {
        private class AgentPrivilegeGrant
        {
            /// <summary>
            ///     Package IDs which require this privilege granted
            /// </summary>
            public List<ChimeraPackageIdentifier> PackageIds { get; } = new();

            /// <summary>
            ///     User who performed the grant
            /// </summary>
            public ChimeraUserIdentifier GranterId { get; set; }

            /// <summary>
            ///     When the grant was performed
            /// </summary>
            public DateTimeOffset Timestamp { get; set; }

            /// <summary>
            ///     A description of the access requirement which was granted (for deduplication)
            /// </summary>
            public string AccessRequirement { get; set; }
        }
    }
}