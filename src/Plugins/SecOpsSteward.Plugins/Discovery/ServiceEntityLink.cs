namespace SecOpsSteward.Plugins.Discovery
{
    public class ServiceEntityLink
    {
        /// <summary>
        ///     Source Service
        /// </summary>
        public DiscoveredServiceConfiguration Source { get; set; }

        /// <summary>
        ///     Link from source
        /// </summary>
        public string SourceLinkContext { get; set; }

        /// <summary>
        ///     Configuration string this link comes from, if present
        /// </summary>
        public string SourceLinkConfigurationName { get; set; }

        // ---

        /// <summary>
        ///     Destination Service
        /// </summary>
        public DiscoveredServiceConfiguration Destination { get; set; }

        /// <summary>
        ///     Link to destination
        /// </summary>
        public string DestinationLinkContext { get; set; }

        /// <summary>
        ///     Configuration string this link comes from, if present
        /// </summary>
        public string DestinationLinkConfigurationName { get; set; }

        public ServiceEntityLink Clone()
        {
            return new()
            {
                Source = Source,
                Destination = Destination,
                SourceLinkContext = SourceLinkContext,
                SourceLinkConfigurationName = SourceLinkConfigurationName,
                DestinationLinkContext = DestinationLinkContext,
                DestinationLinkConfigurationName = DestinationLinkConfigurationName
            };
        }

        public override string ToString()
        {
            return
                $"Svc Link from \"{Source}\" (via \"{SourceLinkContext}\") to \"{Destination}\" (via \"{DestinationLinkContext}\")";
        }
    }
}