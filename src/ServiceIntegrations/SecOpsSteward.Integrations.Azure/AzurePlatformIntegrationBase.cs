using Microsoft.Extensions.Logging;
using SecOpsSteward.Plugins.Azure;
using SecOpsSteward.Shared;
using SecOpsSteward.Shared.Roles;

namespace SecOpsSteward.Integrations.Azure
{
    public class AzurePlatformIntegrationBase
    {
        protected readonly ChimeraServiceConfigurator _configurator;
        protected readonly AzureCurrentCredentialFactory _platformFactory;
        protected readonly IRoleAssignmentService _roleAssignment;
        protected readonly ILogger Logger;

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

        protected string CfgSubscriptionId => _configurator["SubscriptionId"];
        protected string CfgResourceGroup => _configurator["ResourceGroup"];

        protected string BaseScope =>
            "/subscriptions/" + CfgSubscriptionId +
            "/resourceGroups/" + CfgResourceGroup;
    }
}