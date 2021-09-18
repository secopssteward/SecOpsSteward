namespace SecOpsSteward.Plugins
{
    public abstract class PluginRbacRequirements
    {
        public abstract string Description { get; protected set; }

        public virtual string TechnicalDescription { get; } = string.Empty;
    }
}