using SecOpsSteward.Shared.Configuration.Models;

namespace SecOpsSteward.Shared.Configuration
{
    /// <summary>
    ///     Agent Configuration persisted into the configuration store
    /// </summary>
    public class AgentConfiguration
    {
        /// <summary>
        ///     ID of Agent being configured
        /// </summary>
        public ChimeraAgentIdentifier AgentId { get; set; }

        /// <summary>
        ///     Alias of Agent to help users identify it without reading Guids
        /// </summary>
        public string DisplayAlias { get; set; }

        /// <summary>
        ///     List of Access Rules for Users and Packages
        /// </summary>
        public AccessRules AccessRules { get; set; } = new();

        /// <summary>
        ///     List of access scopes and roles granted to an Agent's OID
        /// </summary>
        public AgentPrivilegeGrants GrantedAccess { get; set; } = new();
    }
}