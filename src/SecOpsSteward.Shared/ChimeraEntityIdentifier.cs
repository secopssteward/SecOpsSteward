using System;
using System.Text.Json.Serialization;

namespace SecOpsSteward.Shared
{
    [Flags]
    public enum PackageIdentifierComponents
    {
        Container = 1,
        Service = 2,
        Package = 4
    }

    /// <summary>
    ///     Identifies a Package
    ///     XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX
    ///     First and second segments are the Container (12)
    ///     Third and fourth segments are the Service (8)
    ///     Fifth segment is the Plugin (12)
    /// </summary>
    public class ChimeraPackageIdentifier : ChimeraEntityIdentifier
    {
        private const int CONTAINER_LENGTH = 12;
        private const int SERVICE_LENGTH = 8;
        private const int PLUGIN_LENGTH = 12;

        /// <summary>
        ///     Identifies a Package
        /// </summary>
        public ChimeraPackageIdentifier() : base(EntityType.Package, Guid.Empty)
        {
        }

        /// <summary>
        ///     Identifies a Package
        /// </summary>
        /// <param name="id">Package ID</param>
        public ChimeraPackageIdentifier(Guid id) : base(EntityType.Package, id)
        {
        }

        /// <summary>
        ///     Get Container ID segment from package ID (12)
        /// </summary>
        [JsonIgnore]
        public string ContainerId => Id.ToString().Replace("-", "").Substring(0, CONTAINER_LENGTH);

        /// <summary>
        ///     Get service ID segment from package ID (12)
        /// </summary>
        [JsonIgnore]
        public string ServiceId => Id.ToString().Replace("-", "").Substring(CONTAINER_LENGTH, SERVICE_LENGTH);

        /// <summary>
        ///     Get Plugin ID segment from package ID (8)
        /// </summary>
        [JsonIgnore]
        public string PluginId =>
            Id.ToString().Replace("-", "").Substring(CONTAINER_LENGTH + SERVICE_LENGTH, PLUGIN_LENGTH);

        /// <summary>
        ///     Implicitly converts a Guid to a Package Identifier
        /// </summary>
        /// <param name="g">Guid to convert</param>
        public static implicit operator ChimeraPackageIdentifier(Guid g)
        {
            return new(g);
        }

        public Guid GetComponents(PackageIdentifierComponents component)
        {
            var id = string.Empty;
            if (component.HasFlag(PackageIdentifierComponents.Container)) id += ContainerId;
            else id += new string('0', CONTAINER_LENGTH);

            if (component.HasFlag(PackageIdentifierComponents.Service)) id += ServiceId;
            else id += new string('0', SERVICE_LENGTH);

            if (component.HasFlag(PackageIdentifierComponents.Package)) id += PluginId;
            else id += new string('0', PLUGIN_LENGTH);

            return Guid.Parse(id);
        }
    }

    /// <summary>
    ///     Identifies an Agent
    /// </summary>
    public class ChimeraAgentIdentifier : ChimeraEntityIdentifier
    {
        /// <summary>
        ///     Identifies an Agent
        /// </summary>
        public ChimeraAgentIdentifier() : base(EntityType.Agent, Guid.Empty)
        {
        }

        /// <summary>
        ///     Identifies an Agent
        /// </summary>
        /// <param name="id">Agent ID</param>
        public ChimeraAgentIdentifier(Guid id) : base(EntityType.Agent, id)
        {
        }

        /// <summary>
        ///     Implicitly converts a Guid to an Agent Identifier
        /// </summary>
        /// <param name="g">Guid to convert</param>
        public static implicit operator ChimeraAgentIdentifier(Guid g)
        {
            return new(g);
        }
    }

    /// <summary>
    ///     Identifies a User
    /// </summary>
    public class ChimeraUserIdentifier : ChimeraEntityIdentifier
    {
        /// <summary>
        ///     Identifies a User
        /// </summary>
        public ChimeraUserIdentifier() : base(EntityType.User, Guid.Empty)
        {
        }

        /// <summary>
        ///     Identifies a User
        /// </summary>
        /// <param name="id">User ID</param>
        public ChimeraUserIdentifier(Guid id) : base(EntityType.User, id)
        {
        }

        /// <summary>
        ///     Implicitly converts a Guid to a User Identifier
        /// </summary>
        /// <param name="g">Guid to convert</param>
        public static implicit operator ChimeraUserIdentifier(Guid g)
        {
            return new(g);
        }
    }

    /// <summary>
    ///     Identifies an entity in the Chimera system
    /// </summary>
    public class ChimeraEntityIdentifier
    {
        /// <summary>
        ///     Possible types of entity to be identified
        /// </summary>
        [Flags]
        public enum EntityType
        {
            /// <summary>
            ///     Agent identifier
            /// </summary>
            Agent = 1,

            /// <summary>
            ///     User identifier
            /// </summary>
            User = 2,

            /// <summary>
            ///     Package identifier
            /// </summary>
            Package = 4
        }

        /// <summary>
        ///     Identifies an entity in the Chimera system
        /// </summary>
        public ChimeraEntityIdentifier()
        {
        }

        /// <summary>
        ///     Identifies an entity in the Chimera system
        /// </summary>
        /// <param name="type">Entity type</param>
        /// <param name="id">Entity ID</param>
        public ChimeraEntityIdentifier(EntityType type, Guid id)
        {
            Type = type;
            Id = id;
        }

        /// <summary>
        ///     Type of entity being identified
        /// </summary>
        public EntityType Type { get; set; }

        /// <summary>
        ///     ID of entity
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        ///     Get short representation of Entity ID
        /// </summary>
        public string ShortId => Id.ToString().Substring(0, 8);

        /// <summary>
        ///     Get HashCode for identifier
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(Type, Id);
        }

        /// <summary>
        ///     Serialize identifier to string
        /// </summary>
        /// <returns>
        ///     <c>(Type)-(Id)</c>
        /// </returns>
        public override string ToString()
        {
            return $"{Type.ToString()}-{Id}";
        }

        /// <summary>
        ///     Check if two identifiers are equivalent
        /// </summary>
        /// <param name="obj">Identifier to check equality of</param>
        /// <returns><c>TRUE</c> if the identifiers are equivalent</returns>
        public override bool Equals(object obj)
        {
            if (!(obj is ChimeraEntityIdentifier)) return false;
            return (obj as ChimeraEntityIdentifier).Type == Type &&
                   (obj as ChimeraEntityIdentifier).Id == Id;
        }

        /// <summary>
        ///     Check if two identifiers are equivalent
        /// </summary>
        /// <param name="id1">First identifier</param>
        /// <param name="id2">Second identifier</param>
        /// <returns><c>TRUE</c> if the identifiers are equivalent</returns>
        public static bool operator ==(ChimeraEntityIdentifier id1, ChimeraEntityIdentifier id2)
        {
            if (ReferenceEquals(id1, id2)) return true;
            if (ReferenceEquals(id1, null) ||
                ReferenceEquals(id2, null)) return false;
            return id1.Equals(id2);
        }

        /// <summary>
        ///     Check if two identifiers are NOT equivalent
        /// </summary>
        /// <param name="id1">First identifier</param>
        /// <param name="id2">Second identifier</param>
        /// <returns><c>TRUE</c> if the identifiers are NOT equivalent</returns>
        public static bool operator !=(ChimeraEntityIdentifier id1, ChimeraEntityIdentifier id2)
        {
            if (ReferenceEquals(id1, id2)) return false;
            if (ReferenceEquals(id1, null) ||
                ReferenceEquals(id2, null)) return true;
            return !id1.Equals(id2);
        }
    }

    public static class GuidExtensions
    {
        /// <summary>
        ///     Get short representation of Guid
        /// </summary>
        public static string ShortId(this Guid guid)
        {
            return guid.ToString().Substring(0, 8);
        }
    }
}