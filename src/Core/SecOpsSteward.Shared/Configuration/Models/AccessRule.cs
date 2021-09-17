using System;

namespace SecOpsSteward.Shared.Configuration.Models
{
    public partial class AccessRules
    {
        /// <summary>
        /// A single access rule mapping a User to a Package
        /// </summary>
        private class AccessRule
        {
            /// <summary>
            /// Package ID which User can execute
            /// </summary>
            public ChimeraPackageIdentifier PackageId { get; set; }

            /// <summary>
            /// User ID which can execute the associated Package
            /// </summary>
            public ChimeraUserIdentifier UserId { get; set; }

            public override bool Equals(object obj)
            {
                if (!(obj is AccessRule)) return false;
                if (((AccessRule)obj).PackageId != this.PackageId) return false;
                if (((AccessRule)obj).UserId != this.UserId) return false;
                return true;
            }
            public override int GetHashCode() => HashCode.Combine(PackageId.Id, UserId.Id);
        }
    }
}
