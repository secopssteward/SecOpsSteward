using Microsoft.Extensions.Logging;
using SecOpsSteward.Plugins;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SecOpsSteward.Shared.Roles
{
    public class DummyRoleAssignmentService : IRoleAssignmentService
    {
        public static Guid CURRENT_USER_ID = Guid.Parse("2341d1b7-6827-44f1-bdfc-56c4f1f7db0a");

        public List<Tuple<ChimeraEntityIdentifier, AssignableRole, string>> IdentityRoles { get; set; } = new List<Tuple<ChimeraEntityIdentifier, AssignableRole, string>>();

        private readonly ILogger<DummyRoleAssignmentService> _logger;
        public DummyRoleAssignmentService(ILogger<DummyRoleAssignmentService> logger)
        {
            _logger = logger;
        }

        public Task<bool> ApplyScopedRoleToIdentity(ChimeraEntityIdentifier Entity, AssignableRole Role, string Scope)
        {
            _logger.LogTrace($"Apply role {Role} to {Entity} with scope {Scope}");
            IdentityRoles.Add(Tuple.Create(Entity, Role, Scope));
            return Task.FromResult(true);
        }

        public Task<bool> RemoveScopedRoleFromIdentity(ChimeraEntityIdentifier Entity, AssignableRole Role, string Scope)
        {
            _logger.LogTrace($"Remove role {Role} from {Entity} with scope {Scope}");
            IdentityRoles.Remove(Tuple.Create(Entity, Role, Scope));
            return Task.FromResult(true);
        }

        public Task<TokenOwner> ResolveUsername(string username)
        {
            _logger.LogTrace($"Resolving username {username}");
            return Task.FromResult(new TokenOwner()
            {
                Name = $"User {username}",
                Email = $"{username}@id",
                UserId = new ChimeraUserIdentifier(GetGuidFromUsername(username)),
                Aliases = new List<string>() { $"{username}", $"{username}@Id" }
            });
        }

        private static Guid GetGuidFromUsername(string username)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(Encoding.Default.GetBytes(username));
                return new Guid(hash);
            }
        }

        public Task<bool> HasAssignedPluginRole<TPlugin>(TPlugin plugin, IEnumerable<PluginRbacRequirements> requirements, string identity)
            where TPlugin : IPlugin
        {
            _logger.LogTrace($"Checking for assigned roles for plugin {plugin.GetDescriptiveName()}");
            return Task.FromResult(true);
        }

        public Task AssignPluginRole<TPlugin>(TPlugin plugin, IEnumerable<PluginRbacRequirements> requirements, string identity)
            where TPlugin : IPlugin
        {
            _logger.LogTrace($"c roles for plugin {plugin.GetDescriptiveName()} to {identity}");
            return Task.CompletedTask;
        }

        public Task UnassignPluginRole<TPlugin>(TPlugin plugin, IEnumerable<PluginRbacRequirements> requirements, string identity)
            where TPlugin : IPlugin
        {
            _logger.LogTrace($"Unassigning roles for plugin {plugin.GetDescriptiveName()} to {identity}");
            return Task.CompletedTask;
        }
    }
}
