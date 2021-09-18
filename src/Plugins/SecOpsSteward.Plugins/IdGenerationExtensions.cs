using System;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace SecOpsSteward.Plugins
{
    public static class IdGenerationExtensions
    {
        public static Guid GenerateId(this Type type)
        {
            if (type.GetInterfaces().Contains(typeof(IPlugin))) return GeneratePluginId(type);
            return GenerateManagedServiceId(type);
        }

        public static Guid GenerateId(this IPlugin plugin)
        {
            return GeneratePluginId(plugin.GetType());
        }

        public static Guid GenerateId(this IManagedServicePackage managedService)
        {
            return GenerateManagedServiceId(managedService.GetType());
        }

        private static Guid GeneratePluginId(Type t)
        {
            var fqName = Encoding.UTF8.GetBytes(t.FullName);
            var fqHash = SHA256.Create().ComputeHash(fqName);

            var attr = t.GetCustomAttribute<ManagedServiceAttribute>();
            return new Guid(
                attr.ManagedServiceId.ToByteArray()
                    .Take(4 + 2 + 2 + 2) // first 4 segments (Container-Container-Service-Service)
                    .Concat(fqHash.Take(6)) // Last segment (Plugin)
                    .ToArray());
        }

        private static Guid GenerateManagedServiceId(Type t)
        {
            var serviceFqName = Encoding.UTF8.GetBytes(t.FullName);
            var serviceFqHash = SHA256.Create().ComputeHash(serviceFqName);

            var nsName = Encoding.UTF8.GetBytes(t.Namespace);
            var nsHash = SHA256.Create().ComputeHash(nsName);

            return new Guid(
                nsHash.Take(4 + 2) // first 2 segments (Container-Container)
                    .Concat(serviceFqHash.Take(2 + 2)) // second 2 segments (Service-Service)
                    .Concat(new byte[6]) // last segment is empty (Plugin)
                    .ToArray());
        }

        public static Guid GenerateWorkflowId<IManagedService>(string name)
        {
            var svc = GenerateManagedServiceId(typeof(IManagedService));

            var wfName = Encoding.UTF8.GetBytes(name);
            var wfHash = SHA256.Create().ComputeHash(wfName);

            return new Guid(
                svc.ToByteArray().Take(4 + 2 + 2 + 2) // first 4 segments (Container-Container-Service-Service)
                    .Concat(wfHash.Take(6)) // Last segment (Workflow)
                    .ToArray());
        }
    }
}