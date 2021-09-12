using McMaster.NETCore.Plugins;
using SecOpsSteward.Plugins;
using SecOpsSteward.Plugins.Configurable;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SecOpsSteward.Shared.Packaging.Wrappers
{
    /// <summary>
    /// Wraps Plugin interfaces to handle constructing pointers to methods in the Plugin
    /// </summary>
    public class ContainerWrapper : IDisposable
    {
        /// <summary>
        /// Assembly which is common among Plugins
        /// </summary>
        protected static string SHARED_ASSEMBLY_NAME = "Chimera.Plugins.dll";

        /// <summary>
        /// Types which are shared among all Plugins
        /// </summary>
        protected static Type[] SHARED_TYPES = new[]
        {
            typeof(IManagedServicePackage),
            typeof(IManagedServicePackage<>),
            typeof(IConfigurableObject),
            typeof(IConfigurableObjectConfiguration),
            typeof(ConfigurableObjectParameter),
            typeof(ConfigurableObjectParameterCollection),
            typeof(PluginOutputStructure)
        };

        /// <summary>
        /// Wrapped CLR Assembly
        /// </summary>
        public Assembly Assembly { get; private set; }

        public Dictionary<Guid, ContainerPluginWrapper> Plugins = new Dictionary<Guid, ContainerPluginWrapper>();
        public Dictionary<Guid, ContainerServiceWrapper> Services = new Dictionary<Guid, ContainerServiceWrapper>();
        private PluginLoader _loader;
        private string _fileName;

        public ContainerWrapper(string fileName)
        {
            _fileName = fileName;
        }

        public bool IsValid()
        {
            try { Load(); }
            catch { return false; }
            return Plugins.Any() || Services.Any();
        }

        public ContainerPluginWrapper GetPlugin(Guid pluginId)
        {
            Load();
            return Plugins[pluginId];
        }

        public ContainerServiceWrapper GetService(Guid serviceId)
        {
            Load();
            return Services[serviceId];
        }

        /// <summary>
        /// If the given file is a valid CLR assembly
        /// </summary>
        /// <param name="fileName">File name to test</param>
        /// <returns></returns>
        public virtual bool IsValid(string fileName)
        {
            if (Path.GetFileName(fileName).ToUpper() == SHARED_ASSEMBLY_NAME.ToUpper())
                return false;
            try
            {
                var asmName = AssemblyName.GetAssemblyName(fileName);
                return asmName != null;
            }
            catch { }
            return false;
        }

        /// <summary>
        /// Loads an Assembly and detects the appropriate Types for the Plugin
        /// </summary>
        public void Load()
        {
            _loader = PluginLoader.CreateFromAssemblyFile(_fileName, true, SHARED_TYPES, c =>
            {
                c.IsLazyLoaded = true;
                c.PreferSharedTypes = true;
            });
            var asm = _loader.LoadDefaultAssembly();

            foreach (var pluginType in asm.GetTypes()
                                          .Where(t => t.GetInterface(nameof(IPlugin)) != null)
                                          .Where(t => !t.IsGenericType))
            {
                var plugin = Activator.CreateInstance(pluginType) as IPlugin;
                Plugins[plugin.GenerateId()] = new ContainerPluginWrapper(pluginType);
            }

            foreach (var serviceType in asm.GetTypes()
                                           .Where(t => t.GetInterface(nameof(IManagedServicePackage)) != null)
                                           .Where(t => !t.IsGenericType))
            {
                var msp = Activator.CreateInstance(serviceType) as IManagedServicePackage;
                Services[msp.GenerateId()] = new ContainerServiceWrapper(serviceType);
            }

            Assembly = asm;
        }

        /// <summary>
        /// Disposes a Plugin wrapper, including the loaded assembly
        /// </summary>
        public void Dispose()
        {
            Plugins.Clear();
            Services.Clear();

            _loader.Dispose();
        }
    }
}
