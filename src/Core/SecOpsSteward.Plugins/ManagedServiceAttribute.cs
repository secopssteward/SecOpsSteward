using System;

namespace SecOpsSteward.Plugins
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ManagedServiceAttribute : Attribute
    {
        public Type ManagedServiceType { get; set; }
        public Guid ManagedServiceId { get; private set; }

        public ManagedServiceAttribute(Type serviceType)
        {
            ManagedServiceType = serviceType;
            ManagedServiceId = serviceType.GenerateId();
        }
    }
}
