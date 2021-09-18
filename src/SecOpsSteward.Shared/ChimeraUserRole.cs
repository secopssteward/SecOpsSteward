using System;

namespace SecOpsSteward.Shared
{
    [Flags]
    public enum ChimeraUserRole
    {
        None = 0,

        /// <summary>
        /// Can do everything in the system
        /// </summary>
        GlobalAdmin = AgentAdmin | UserAdmin | MessageAdmin | MessageDispatcher | PackageAdmin,

        /// <summary>
        /// Can enroll and manage agents
        /// </summary>
        AgentAdmin = 1, // RW agentkey/agentcfg vault

        /// <summary>
        /// Can enroll and manage users
        /// </summary>
        UserAdmin = 2, // RW userkey vault, make delegations

        /// <summary>
        /// Can dispatch/receive messages
        /// </summary>
        MessageAdmin = 4, // RW entire msg queue

        /// <summary>
        /// Can add and remove packages
        /// </summary>
        PackageAdmin = 8, // RW packages

        /// <summary>
        /// Can dispatch/receive messages
        /// </summary>
        MessageDispatcher = 16, // RW msg queue
    }
}
