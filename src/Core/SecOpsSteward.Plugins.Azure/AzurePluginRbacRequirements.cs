namespace SecOpsSteward.Plugins.Azure
{
    public class AzurePluginRbacRequirements : PluginRbacRequirements
    {
        public override string Description { get; protected set; }

        public string[] Actions { get; protected set; }
        public string[] DataActions { get; protected set; }

        public string Scope { get; protected set; }

        protected AzurePluginRbacRequirements(
            string description, string scope)
        {
            Description = description;
            Scope = scope;
        }

        public static AzurePluginRbacRequirements WithActions(
            string description,
            string scope,
            params string[] actions) =>
            new AzurePluginRbacRequirements(description, scope)
            {
                Actions = actions
            };

        public static AzurePluginRbacRequirements WithDataActions(
            string description,
            string scope,
            params string[] dataActions) =>
            new AzurePluginRbacRequirements(description, scope)
            {
                DataActions = dataActions
            };

        public static AzurePluginRbacRequirements WithActionsAndDataActions(
            string description,
            string scope,
            string[] actions,
            string[] dataActions) =>
            new AzurePluginRbacRequirements(description, scope)
            {
                Actions = actions,
                DataActions = dataActions
            };
    }
}
