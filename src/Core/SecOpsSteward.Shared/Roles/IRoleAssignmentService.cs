using Microsoft.AspNetCore.Components.Authorization;
using SecOpsSteward.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SecOpsSteward.Shared.Roles
{
    public enum AssignableRole
    {
        CanDoAllOnVault,
        CanManagePermissionsOnVault,
        CanCreateKeys,

        CanReadSecrets,
        CanReadWriteSecrets,

        CanValidateEncryptKey,
        CanSignDecryptKey,

        CanReceiveFromQueue,
        CanSendToQueue,

        CanReadPackages,
        CanReadWritePackages
    }

    /// <summary>
    /// Executed when roles or groups must be modified or are given/revoked permissions
    /// </summary>
    public interface IRoleAssignmentService
    {
        /// <summary>
        /// Apply a role and scope to a given Entity
        /// </summary>
        /// <param name="Entity">Entity to receive role and scope</param>
        /// <param name="Role">Role to apply</param>
        /// <param name="Scope">Scope of role</param>
        /// <returns></returns>
        Task<bool> ApplyScopedRoleToIdentity(ChimeraEntityIdentifier Entity, AssignableRole Role, string Scope);

        /// <summary>
        /// Remove a role and scope from a given Entity
        /// </summary>
        /// <param name="Entity">Entity to remove role and scope from</param>
        /// <param name="Role">Role to remove</param>
        /// <param name="Scope">Scope of role</param>
        /// <returns></returns>
        Task<bool> RemoveScopedRoleFromIdentity(ChimeraEntityIdentifier Entity, AssignableRole Role, string Scope);

        /// <summary>
        /// Resolve a username to its identity and display name
        /// </summary>
        /// <param name="username">Username to resolve</param>
        /// <returns>Entity's ID and display name</returns>
        Task<TokenOwner> ResolveUsername(string username);

        Task<bool> HasAssignedPluginRole<TPlugin>(TPlugin plugin, IEnumerable<PluginRbacRequirements> requirements, string identity)
            where TPlugin : IPlugin;
        Task AssignPluginRole<TPlugin>(TPlugin plugin, IEnumerable<PluginRbacRequirements> requirements, string identity)
            where TPlugin : IPlugin;
        Task UnassignPluginRole<TPlugin>(TPlugin plugin, IEnumerable<PluginRbacRequirements> requirements, string identity)
            where TPlugin : IPlugin;
    }


    /// <summary>
    /// Represents an Entity in the system
    /// </summary>
    public class TokenOwner
    {
        /// <summary>
        /// Display name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Email address
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Avatar link
        /// </summary>
        public string Avatar { get; set; }

        /// <summary>
        /// Entity ID
        /// </summary>
        public ChimeraUserIdentifier UserId { get; set; }

        /// <summary>
        /// Aliases for this user
        /// </summary>
        public List<string> Aliases { get; set; } = new List<string>();

        public static TokenOwner Create(AuthenticationState state, bool hasAuthConfiguration)
        {
            if (!hasAuthConfiguration) return new TokenOwner() { Name = "Local User", Email = "user@_local_", UserId = Guid.NewGuid(), Avatar = "" };
            if (!state.User.Identity.IsAuthenticated) return new TokenOwner() { };

            var hash = System.Security.Cryptography.MD5.Create().ComputeHash(System.Text.Encoding.UTF8.GetBytes(state.User.Identity.Name));
            var gravatar = "https://gravatar.com/avatar/" + BitConverter.ToString(hash).Replace("-", "").ToLower();

            return new TokenOwner()
            {
                Name = state.User.Claims.First(c => c.Type == "name").Value,
                Email = state.User.Identity.Name,
                UserId = Guid.Parse(state.User.Claims.First(c => c.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Value),
                Avatar = gravatar
            };
        }
    }

    /// <summary>
    /// Represents an Entity in the system wiht its admin role, if present
    /// </summary>
    public class TokenOwnerWithRole : TokenOwner
    {
        /// <summary>
        /// If the entity is an Administrator
        /// </summary>
        public bool IsAdmin { get; set; }
    }
}
