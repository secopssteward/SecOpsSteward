using System.Linq;

namespace SecOpsSteward.Plugins.Azure
{
    public class AzurePluginRbacRequirements : PluginRbacRequirements
    {
        protected AzurePluginRbacRequirements(
            string description, string scope)
        {
            Description = description;
            Scope = scope;
        }

        public override string Description { get; protected set; }

        public string[] Actions { get; protected set; } = new string[0];
        public string[] DataActions { get; protected set; } = new string[0];

        public string Scope { get; protected set; }

        public override string TechnicalDescription
        {
            get
            {
                var str = $"On Scope '{Scope}':\n";
                if (Actions.Any()) str += "Actions:\n * " + string.Join("\n * ", Actions) + "\n";
                if (DataActions.Any()) str += "Data Actions:\n * " + string.Join("\n * ", DataActions) + "\n";
                return str;
            }
        }

        public static AzurePluginRbacRequirements WithActions(
            string description,
            string scope,
            params string[] actions)
        {
            return new(description, scope)
            {
                Actions = actions
            };
        }

        public static AzurePluginRbacRequirements WithDataActions(
            string description,
            string scope,
            params string[] dataActions)
        {
            return new(description, scope)
            {
                DataActions = dataActions
            };
        }

        public static AzurePluginRbacRequirements WithActionsAndDataActions(
            string description,
            string scope,
            string[] actions,
            string[] dataActions)
        {
            return new(description, scope)
            {
                Actions = actions,
                DataActions = dataActions
            };
        }
    }
}