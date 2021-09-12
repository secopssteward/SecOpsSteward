namespace SecOpsSteward.Plugins.Discovery
{
    public class ServiceEntityLink
    {
        public DiscoveredServiceConfiguration Source { get; set; }
        public string SourceLinkContext { get; set; }
        public string SourceLinkConfigurationName { get; set; }

        public DiscoveredServiceConfiguration Destination { get; set; }
        public string DestinationLinkContext { get; set; }
        public string DestinationLinkConfigurationName { get; set; }

        public ServiceEntityLink Clone() =>
            new ServiceEntityLink()
            {
                Source = Source,
                Destination = Destination,
                SourceLinkContext = SourceLinkContext,
                SourceLinkConfigurationName = SourceLinkConfigurationName,
                DestinationLinkContext = DestinationLinkContext,
                DestinationLinkConfigurationName = DestinationLinkConfigurationName
            };

        public override string ToString()
        {
            return $"Svc Link from \"{Source}\" (via \"{SourceLinkContext}\") to \"{Destination}\" (via \"{DestinationLinkContext}\")";
        }
    }
}
