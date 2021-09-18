using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;
using Microsoft.Rest;

namespace SecOpsSteward.Plugins.Azure
{
    public static class AzureCurrentCredentialFactoryExtensions
    {
        public static void RegisterCurrentCredentialFactory(this IServiceCollection services, string tenantId,
            string subscriptionId, bool useManagedId = false, bool useDummy = false)
        {
            services.AddScoped(s =>
                new AzureCurrentCredentialFactory(s, tenantId, subscriptionId, useManagedId, useDummy));
        }
    }

    public class AzureCurrentCredentialFactory
    {
        private readonly IServiceProvider _sp;
        private readonly string _subscriptionId;
        private readonly string _tenantId;
        private readonly bool _useEmulatedCredential;
        private readonly bool _useManagedIdentity;
        private readonly Dictionary<string, AzureCurrentCredential> _handles = new();

        public AzureCurrentCredentialFactory(
            IServiceProvider sp,
            string tenantId,
            string subscriptionId,
            bool useManagedIdentity = false,
            bool useEmulatedCredential = false)
        {
            _sp = sp;
            _tenantId = tenantId;
            _subscriptionId = subscriptionId;
            _useManagedIdentity = useManagedIdentity;
            _useEmulatedCredential = useEmulatedCredential;
        }

        public void RegisterManualCredentialHandle(TokenCredential credential, string subscriptionId = null)
        {
            if (subscriptionId == null) subscriptionId = _subscriptionId;
            _handles.Add(subscriptionId, new CurrentUserCredential(credential, _tenantId, subscriptionId));
        }

        public AzureCurrentCredential GetCredentialPreferringAppIdentity()
        {
            return GetCredentialPreferringAppIdentity(_subscriptionId);
        }

        public AzureCurrentCredential GetCredentialPreferringAppIdentity(string subscriptionId)
        {
            // todo: rework this
            return new CurrentAgentCredential(_tenantId, _subscriptionId);
        }

        public AzureCurrentCredential GetCredential()
        {
            return GetCredential(_subscriptionId);
        }

        public AzureCurrentCredential GetCredential(string subscriptionId)
        {
            if (_useEmulatedCredential) return new EmulatedCurrentCredential();
            lock (_handles)
            {
                if (!_handles.ContainsKey(_subscriptionId))
                {
                    AzureCurrentCredential handle;
                    if (_useManagedIdentity)
                        handle = new CurrentAgentCredential(_tenantId, _subscriptionId);
                    else
                        handle = CurrentUserCredential.CreateCredentialWithConsentHandler(_sp, _tenantId,
                            subscriptionId);

                    if (handle != null) _handles[_subscriptionId] = handle;
                    else return null;
                }

                return _handles[_subscriptionId];
            }
        }
    }

    public class EmulatedCurrentCredential : AzureCurrentCredential
    {
        public EmulatedCurrentCredential()
        {
            TenantId = Guid.NewGuid().ToString();
            SubscriptionId = Guid.NewGuid().ToString();
            Credential = null;
            _emulated = true;
        }
    }

    public abstract class AzureCurrentCredential
    {
        public static string[] RequiredScopes =
        {
            "https://management.azure.com/user_impersonation", // For general Azure management
            "https://graph.microsoft.com/user.read", // For user indexing
            "https://vault.azure.net/user_impersonation", // For access to keys and secrets for crypto
            "https://servicebus.azure.net/user_impersonation" // For messaging
        };

        protected IAzure _azure;
        protected bool _emulated;
        private string _subscriptionId;
        private string _tenantId;

        public TokenCredential Credential { get; protected set; }

        public string TenantId
        {
            get => _tenantId;
            set
            {
                _tenantId = value;
                _azure = null;
            }
        }

        public string SubscriptionId
        {
            get => _subscriptionId;
            set
            {
                _subscriptionId = value;
                _azure = null;
            }
        }

        public IAzure GetAzure()
        {
            if (_emulated) return null; // todo: mock?
            if (_azure == null)
            {
                var armToken = Credential
                    .GetToken(new TokenRequestContext(new[] {"https://management.azure.com/.default"}, null), default)
                    .Token;
                var armCreds = new TokenCredentials(armToken);

                var graphToken = Credential
                    .GetToken(new TokenRequestContext(new[] {"https://graph.windows.net/.default"}, null), default)
                    .Token;
                var graphCreds = new TokenCredentials(graphToken);

                // Note that the deficiency in IAzure is that it lacks key vault client support.
                // We have to provide separate client instances authed against the TokenCredential.

                var creds = new AzureCredentials(armCreds, graphCreds, _tenantId, AzureEnvironment.AzureGlobalCloud)
                    .WithDefaultSubscription(_subscriptionId);

                _azure = Microsoft.Azure.Management.Fluent.Azure.Authenticate(creds)
                    .WithSubscription(_subscriptionId);
            }

            return _azure;
        }
    }

    public class CurrentAgentCredential : AzureCurrentCredential
    {
        public CurrentAgentCredential(string tenantId, string subscriptionId)
        {
            TenantId = tenantId;
            SubscriptionId = subscriptionId;
            Credential = new DefaultAzureCredential();
        }
    }

    public class CurrentUserCredential : AzureCurrentCredential
    {
        public CurrentUserCredential(TokenCredential credential, string tenantId, string subscriptionId)
        {
            Credential = credential;
            TenantId = tenantId;
            SubscriptionId = subscriptionId;
        }

        public static AzureCurrentCredential CreateCredentialWithConsentHandler(IServiceProvider services,
            string tenantId, string servicesSubscriptionId = null)
        {
            var tokenAcquisition = services.GetRequiredService<ITokenAcquisition>();
            var consentHandler = services.GetRequiredService<MicrosoftIdentityConsentAndConditionalAccessHandler>();

            try
            {
                Task.WhenAll(RequiredScopes.Select(item => tokenAcquisition.GetAccessTokenForUserAsync(new[] {item})))
                    .Wait();
            }
            catch (Exception e)
            {
                consentHandler.HandleException(e);
                return null;
            }

            var cred = new TokenAcquisitionTokenCredential(tokenAcquisition);

            return new CurrentUserCredential(cred, tenantId, servicesSubscriptionId);
        }
    }
}