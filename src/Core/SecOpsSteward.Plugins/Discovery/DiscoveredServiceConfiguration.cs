﻿using SecOpsSteward.Plugins.Configurable;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SecOpsSteward.Plugins.Discovery
{
    public class DiscoveredServiceConfiguration
    {
        /// <summary>
        /// Used by sequencer - no need to be manually populated
        /// TODO: Remove this element for plugin devs
        /// </summary>
        public IManagedServicePackage EntityService { get; set; } = null;
        public Guid ManagedServiceId { get; set; }

        /// <summary>
        /// What this configuration is pointing to, in a user-friendly description
        /// </summary>
        public string DescriptiveName { get; set; }

        /// <summary>
        /// Main identifier for discovered resource
        /// </summary>
        public string Identifier { get; set; }

        /// <summary>
        /// Links to other discovered elements which are confirmed
        /// </summary>
        public List<ServiceEntityLink> ResolvedOutgoingLinks { get; set; } = new List<ServiceEntityLink>();

        /// <summary>
        /// Configuration necessary to access this resource/service
        /// </summary>
        public IConfigurableObjectConfiguration Configuration { get; set; }

        public List<string> LinksOutTo { get; set; } = new List<string>();
        public List<string> LinksInAs { get; set; } = new List<string>();
        public Dictionary<string, string> ConfigurationValues { get; set; } = new Dictionary<string, string>();

        public List<ServiceEntityLink> GetOutgoingLinks()
        {
            var links = new List<ServiceEntityLink>();
            foreach (var linkOut in LinksOutTo)
            {
                links.Add(new ServiceEntityLink()
                {
                    Source = this,
                    SourceLinkContext = linkOut
                });
            }
            foreach (var configOut in ConfigurationValues)
            {
                links.Add(new ServiceEntityLink()
                {
                    Source = this,
                    SourceLinkConfigurationName = configOut.Key,
                    SourceLinkContext = configOut.Value
                });
            }
            return links;
        }

        public ServiceEntityLink ResolveLinkToService(ServiceEntityLink sourceDetail)
        {
            // check all ways service can be accessed externally against an incoming link
            foreach (var link in LinksInAs.Where(l => l != null))
            {
                // TODO: Make this less naive?
                if (link.Contains(sourceDetail.SourceLinkContext) || sourceDetail.SourceLinkContext.Contains(link))
                {
                    var clone = sourceDetail.Clone();
                    clone.Destination = this;
                    clone.DestinationLinkContext = link;
                    return clone;
                }
            }
            return null;
        }

        public DiscoveredServiceConfiguration Clone()
        {
            return new DiscoveredServiceConfiguration()
            {
                Configuration = Configuration.Clone(),
                DescriptiveName = DescriptiveName,
                EntityService = EntityService,
                Identifier = Identifier,
                LinksInAs = new List<string>(LinksInAs),
                LinksOutTo = new List<string>(LinksOutTo),
                ManagedServiceId = ManagedServiceId,
                ResolvedOutgoingLinks = new List<ServiceEntityLink>(ResolvedOutgoingLinks)
            };
        }

        public override bool Equals(object obj)
        {
            if (obj is not DiscoveredServiceConfiguration) return false;
            var castObj = (DiscoveredServiceConfiguration)obj;

            return this.Identifier == castObj.Identifier;
        }

        public override int GetHashCode() => HashCode.Combine(Identifier);

        public override string ToString()
        {
            return $"Svc Config - \"{DescriptiveName}\" ({Identifier}) - {(EntityService != null ? EntityService.GetDescriptiveName() : "No Svc Mapped Yet")}";
        }
    }
}
