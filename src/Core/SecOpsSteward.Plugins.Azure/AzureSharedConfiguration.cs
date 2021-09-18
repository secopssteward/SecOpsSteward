using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using SecOpsSteward.Plugins.Configurable;

namespace SecOpsSteward.Plugins.Azure
{
    public class AzureSharedConfiguration : IConfigurableObjectConfiguration
    {
        [Required]
        [IdentifiesTargetGrantScope]
        [DisplayName("Tenant ID")]
        public string TenantId { get; set; }

        [Required]
        [IdentifiesTargetGrantScope]
        [DisplayName("Subscription ID")]
        public string SubscriptionId { get; set; }

        [Required]
        [IdentifiesTargetGrantScope]
        [DisplayName("Resource Group")]
        public string ResourceGroup { get; set; }
    }
}