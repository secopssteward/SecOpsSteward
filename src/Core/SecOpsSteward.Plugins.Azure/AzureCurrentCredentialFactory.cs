using Azure.Core;
using Azure.Identity;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SecOpsSteward.Plugins.Azure
{
    public static class AzureCurrentCredentialFactoryExtensions
    {
        public static void RegisterCurrentCredentialFactory(this IServiceCollection services, string tenantId, string subscriptionId, bool useManagedId = false)
        {
            services.AddScoped<AzureCurrentCredentialFactory>(s => new AzureCurrentCredentialFactory(s, tenantId, subscriptionId, useManagedId));
        }
    }

    public class AzureCurrentCredentialFactory
    {
        private Dictionary<string, AzureCurrentCredential> _handles = new Dictionary<string, AzureCurrentCredential>();

        private readonly IServiceProvider _sp;
        private readonly string _tenantId;
        private readonly string _subscriptionId;
        private readonly bool _useManagedIdentity;
        public AzureCurrentCredentialFactory(
            IServiceProvider sp,
            string tenantId,
            string subscriptionId,
            bool useManagedIdentity = false)
        {
            _sp = sp;
            _tenantId = tenantId;
            _subscriptionId = subscriptionId;
            _useManagedIdentity = useManagedIdentity;
        }

        public void RegisterManualCredentialHandle(TokenCredential credential, string subscriptionId = null)
        {
            if (subscriptionId == null) subscriptionId = _subscriptionId;
            _handles.Add(subscriptionId, new CurrentUserCredential(credential, _tenantId, subscriptionId));
        }

        public AzureCurrentCredential GetCredentialPreferringAppIdentity() => GetCredentialPreferringAppIdentity(_subscriptionId);
        public AzureCurrentCredential GetCredentialPreferringAppIdentity(string subscriptionId)
        {
            // todo: rework this
            return new CurrentAgentCredential(_tenantId, _subscriptionId);
        }

        public AzureCurrentCredential GetCredential() => GetCredential(_subscriptionId);
        public AzureCurrentCredential GetCredential(string subscriptionId)
        {
            lock (_handles)
            {
                if (!_handles.ContainsKey(_subscriptionId))
                {
                    AzureCurrentCredential handle;
                    if (_useManagedIdentity)
                        handle = new CurrentAgentCredential(_tenantId, _subscriptionId);
                    else
                        handle = CurrentUserCredential.CreateCredentialWithConsentHandler(_sp, _tenantId, subscriptionId);

                    if (handle != null) _handles[_subscriptionId] = handle;
                    else return null;
                }

                return _handles[_subscriptionId];
            }
        }
    }

    public abstract class AzureCurrentCredential
    {
        public static string[] RequiredScopes = new[]
        {
            "https://management.azure.com/user_impersonation", // For general Azure management
            "https://graph.microsoft.com/user.read",           // For user indexing
            "https://vault.azure.net/user_impersonation",      // For access to keys and secrets for crypto
            "https://servicebus.azure.net/user_impersonation"  // For messaging
        };

        public TokenCredential Credential { get; protected set; }

        protected IAzure _azure;
        private string _tenantId;
        private string _subscriptionId;

        public string TenantId
        {
            get => _tenantId;
            set { _tenantId = value; _azure = null; }
        }

        public string SubscriptionId
        {
            get => _subscriptionId;
            set { _subscriptionId = value; _azure = null; }
        }

        public IAzure GetAzure()
        {
            if (_azure == null)
            {
                var armToken = Credential.GetToken(new TokenRequestContext(scopes: new[] { "https://management.azure.com/.default" }, parentRequestId: null), default).Token;
                var armCreds = new Microsoft.Rest.TokenCredentials(armToken);

                var graphToken = Credential.GetToken(new TokenRequestContext(scopes: new[] { "https://graph.windows.net/.default" }, parentRequestId: null), default).Token;
                var graphCreds = new Microsoft.Rest.TokenCredentials(graphToken);

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

        public static AzureCurrentCredential CreateCredentialWithConsentHandler(IServiceProvider services, string tenantId, string servicesSubscriptionId = null)
        {
            var tokenAcquisition = services.GetRequiredService<ITokenAcquisition>();
            var consentHandler = services.GetRequiredService<MicrosoftIdentityConsentAndConditionalAccessHandler>();

            try
            {
                Task.WhenAll(AzureCurrentCredential.RequiredScopes.Select(item => tokenAcquisition.GetAccessTokenForUserAsync(new[] { item }))).Wait();
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
