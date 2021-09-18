using System;

namespace SecOpsSteward.Plugins
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ManagedServiceAttribute : Attribute
    {
        public ManagedServiceAttribute(Type serviceType)
        {
            ManagedServiceType = serviceType;
            ManagedServiceId = serviceType.GenerateId();
        }

        public Type ManagedServiceType { get; set; }
        public Guid ManagedServiceId { get; }
    }
}