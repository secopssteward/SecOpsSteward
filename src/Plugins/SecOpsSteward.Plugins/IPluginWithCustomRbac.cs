using System.Threading.Tasks;

namespace SecOpsSteward.Plugins
{
    public interface IPluginWithCustomRbac : IPlugin
    {
        /// <summary>
        ///     Grant the access necessary for the given identity to execute the plugin with the loaded configuration
        /// </summary>
        /// <param name="identity">Identity to grant</param>
        /// <returns></returns>
        Task Grant(string identity);

        /// <summary>
        ///     Revoke the access to execute the plugin with the loaded configuration
        /// </summary>
        /// <param name="identity">Identity to revoke</param>
        /// <returns></returns>
        Task Revoke(string identity);

        /// <summary>
        ///     Detect if the identity has rights to execute the plugin with the loaded configuration
        /// </summary>
        /// <param name="identity">Identity to check</param>
        /// <returns></returns>
        Task<bool> HasRights(string identity);
    }
}