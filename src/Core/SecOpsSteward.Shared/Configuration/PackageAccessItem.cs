using System;

namespace SecOpsSteward.Shared.Configuration
{
    /// <summary>
    /// A single access rule mapping a User to a Package
    /// </summary>
    public class PackageAccessItem
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
            if (!(obj is PackageAccessItem)) return false;
            if (((PackageAccessItem)obj).PackageId != this.PackageId) return false;
            if (((PackageAccessItem)obj).UserId != this.UserId) return false;
            return true;
        }
        public override int GetHashCode() => HashCode.Combine(PackageId.Id, UserId.Id);
    }
}
