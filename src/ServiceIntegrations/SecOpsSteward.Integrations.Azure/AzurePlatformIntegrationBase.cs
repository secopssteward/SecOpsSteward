using Microsoft.Extensions.Logging;
using SecOpsSteward.Plugins.Azure;
using SecOpsSteward.Shared;
using SecOpsSteward.Shared.Roles;

namespace SecOpsSteward.Integrations.Azure
{
    public class AzurePlatformIntegrationBase
    {
        protected readonly ILogger Logger;

        protected readonly ChimeraServiceConfigurator _configurator;
        protected readonly IRoleAssignmentService _roleAssignment;
        protected readonly AzureCurrentCredentialFactory _platformFactory;

        protected string CfgSubscriptionId => _configurator["SubscriptionId"];
        protected string CfgResourceGroup => _configurator["ResourceGroup"];

        protected AzurePlatformIntegrationBase(
            ILogger logger,
            ChimeraServiceConfigurator configurator,
            IRoleAssignmentService roleAssignment,
            AzureCurrentCredentialFactory platformFactory)
        {
            Logger = logger;
            _configurator = configurator;
            _roleAssignment = roleAssignment;
            _platformFactory = platformFactory;
        }

        protected AzurePlatformIntegrationBase(
            ILogger logger,
            ChimeraServiceConfigurator configurator,
            AzureCurrentCredentialFactory platformFactory)
        {
            Logger = logger;
            _configurator = configurator;
            _platformFactory = platformFactory;
        }

        protected string BaseScope =>
            "/subscriptions/" + CfgSubscriptionId +
            "/resourceGroups/" + CfgResourceGroup;
    }
}
