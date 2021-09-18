using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SecOpsSteward.Plugins;
using SecOpsSteward.Plugins.Configurable;
using SecOpsSteward.Plugins.Discovery;
using SecOpsSteward.Plugins.WorkflowTemplates;

namespace SecOpsSteward.Shared.DiscoveryWorkflow
{
    public class DiscoverySequencerService
    {
        private static readonly Regex ConfigurationInsertPattern = new(@"\&\&\/(?<name>.*)\/(?<value>.*)");
        private readonly ILogger<DiscoverySequencerService> _logger;
        private readonly IServiceProvider _services;

        public DiscoverySequencerService(IServiceProvider services, ILogger<DiscoverySequencerService> logger)
        {
            _logger = logger;
            _services = services;
        }

        // ----------------------------------------------------------------------------------------------------
        // ---------------------------------- SERVICE ENUMERATION ---------------------------------------------
        // ----------------------------------------------------------------------------------------------------
        public async Task<List<DiscoveredServiceConfiguration>> EnumerateServices(
            IEnumerable<IManagedServicePackage> services)
        {
            // Get first-level discovered configurations
            var results = (await Task.WhenAll(services.Select(s => Task.Run(async () =>
                {
                    try
                    {
                        return await s.Discover();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogCritical($"Error running discovery for '{s.GetDescriptiveName()}'", ex);
                    }

                    return new List<DiscoveredServiceConfiguration>();
                })))).SelectMany(s => s)
                .Where(s => s != null)
                .ToList();

            // Check for specific configurations based on the first pass
            var moreResults = (await Task.WhenAll(services.Select(s => Task.Run(async () =>
                {
                    try
                    {
                        return await s.Discover(results);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogCritical($"Error running second-level discovery for '{s.GetDescriptiveName()}'", ex);
                    }

                    return new List<DiscoveredServiceConfiguration>();
                })))).SelectMany(s => s)
                .Where(s => s != null)
                .ToList();

            // TODO: Recurse until we don't get any results?

            results.AddRange(moreResults);
            results = results.Distinct().ToList();

            results.ForEach(r => r.LinksInAs.RemoveAll(l => string.IsNullOrEmpty(l)));
            results.ForEach(r => r.LinksOutTo.RemoveAll(l => string.IsNullOrEmpty(l)));

            foreach (var item in results) // apply services
                item.EntityService = services.First(s => s.GenerateId() == item.ManagedServiceId);

            return results;
        }


        // ----------------------------------------------------------------------------------------------------
        // ------------------------------ GENERATE SERVICE LINKS AND PATHS ------------------------------------
        // ----------------------------------------------------------------------------------------------------
        public List<List<DiscoveredServiceConfiguration>> CreateServiceLinksAndPaths(
            List<DiscoveredServiceConfiguration> discoveredServices)
        {
            // identify links between 2 elements
            foreach (var entity in discoveredServices)
            {
                var outgoing = entity.GetOutgoingLinks();
                foreach (var match in discoveredServices.Except(new[] {entity}))
                    entity.ResolvedOutgoingLinks.AddRange(outgoing.Select(o => match.ResolveLinkToService(o)));
                entity.ResolvedOutgoingLinks.RemoveAll(l => l == null);
            }

            // generate end-to-end N-element paths from the links
            var paths = new List<List<DiscoveredServiceConfiguration>>();
            var roots = discoveredServices.Where(de => !de.ResolvedOutgoingLinks.Any(cl => cl.Destination == de));
            foreach (var root in roots)
                paths.AddRange(GetPaths(root));
            paths.RemoveAll(p => p.Count < 2);

            // remove duplicates
            paths = paths.Distinct().ToList();

            return paths;
        }

        private static IEnumerable<List<DiscoveredServiceConfiguration>> GetPaths(DiscoveredServiceConfiguration root)
        {
            var queue = new Queue<Tuple<List<DiscoveredServiceConfiguration>, DiscoveredServiceConfiguration>>();
            queue.Enqueue(Tuple.Create(new List<DiscoveredServiceConfiguration>(), root));

            while (queue.Any())
            {
                var node = queue.Dequeue();
                node.Item1.Add(node.Item2);
                if (node.Item2.ResolvedOutgoingLinks.Any())
                    foreach (var child in node.Item2.ResolvedOutgoingLinks)
                        queue.Enqueue(Tuple.Create(node.Item1, child.Destination));
                else
                    yield return node.Item1;
            }
        }


        // ----------------------------------------------------------------------------------------------------
        // --------------------------------------- SUGGEST TEMPLATES ------------------------------------------
        // ----------------------------------------------------------------------------------------------------
        public List<RouteSegmentPossibility> GetTemplateSuggestions(List<DiscoveredServiceConfiguration> path)
        {
            var pathQueue = new Queue<DiscoveredServiceConfiguration>(path);
            return RecursivelyGenerateSegmentsFromServices(pathQueue);
        }

        /// <summary>
        ///     Generates a route segment from a template without any mapped descendants (i.e. this is one single option)
        /// </summary>
        /// <param name="service">Service to generate template for</param>
        /// <param name="templateDefinitionPrototype">Workflow template definition to generate</param>
        /// <returns></returns>
        private RouteSegmentPossibility GenerateTemplateWithoutDependencies(DiscoveredServiceConfiguration service,
            WorkflowTemplateDefinition templateDefinitionPrototype)
        {
            var templateDefinition = templateDefinitionPrototype.Clone();
            var segment = new RouteSegmentPossibility();
            foreach (var value in templateDefinition.Configuration.Parameters)
                segment.TemplateConfigurationValues[value.Name] = value.Value;

            // TODO:
            // We have knowledge of resolved links between things and where those links resolve
            // How to propogate things like AppSetting name into the template?

            foreach (var templateParticipant in templateDefinition.Participants)
            {
                templateParticipant.ServiceConfiguration = service.Clone();
                segment.Add(templateParticipant);

                if (templateParticipant.PackageId == Guid.Empty)
                    continue;

                var participantPlugin =
                    service.EntityService.CreatePlugin(_services, templateParticipant.PackageId, service.Configuration);
                var pluginConfigWithServiceValues = participantPlugin.GetConfigurationObject();
                segment.RouteSegmentOutputs.AddRange(participantPlugin.GetGeneratedSharedOutputs()
                    .Select(o => pluginConfigWithServiceValues.PopulateStringTemplate(o)));
                segment.RouteSegmentInputs.AddRange(participantPlugin.GetRequiredSharedInputs());
            }

            segment.RouteSegmentOutputs = segment.RouteSegmentOutputs.Distinct().ToList();

            return segment;
        }

        private List<RouteSegmentPossibility> RecursivelyGenerateSegmentsFromServices(
            Queue<DiscoveredServiceConfiguration> servicesInOrder,
            List<string> outputsAvailableSoFar = null)
        {
            if (outputsAvailableSoFar == null) outputsAvailableSoFar = new List<string>();

            var allSegments = new List<RouteSegmentPossibility>();
            var serviceConfiguration = servicesInOrder.Dequeue();

            foreach (var template in serviceConfiguration.EntityService.Templates)
            {
                var segmentsGeneratedThisPass = new List<RouteSegmentPossibility>();

                // generates template with the gap
                var generatedSegment = GenerateTemplateWithoutDependencies(serviceConfiguration, template);

                if (generatedSegment.Any(m => m.PackageId == Guid.Empty))
                {
                    var idx = generatedSegment.IndexOf(generatedSegment.First(m => m.PackageId == Guid.Empty));
                    generatedSegment.RemoveAt(idx); // replace the empty step

                    // generate any possible descendants (recursively)
                    var options = RecursivelyGenerateSegmentsFromServices(
                        new Queue<DiscoveredServiceConfiguration>(servicesInOrder),
                        new List<string>(outputsAvailableSoFar));

                    foreach (var opt in options)
                    {
                        // don't add segment option to set if required inputs aren't present
                        if (opt.RouteSegmentInputs.Except(outputsAvailableSoFar).Any())
                            continue;

                        // clone the current segment (the "parent" of all descendants)
                        // and prepend it to the generated options, incl. outputs
                        var clone = generatedSegment.Clone();
                        clone.InsertRange(idx, opt);
                        clone.RouteSegmentOutputs.AddRange(opt.RouteSegmentOutputs);
                        segmentsGeneratedThisPass.Add(clone);
                    }

                    foreach (var segment in segmentsGeneratedThisPass)
                    {
                        // get most recent output value from the segment, if there is one
                        var value = segment.RouteSegmentOutputs.FirstOrDefault();
                        if (value == null) continue;
                        var stringVariableName = "{{$" + value + "}}";

                        // TODO: APPLY CONNECTION STRING TEMPLATE TO VALUE HERE
                        // ** This can come from the resolved link information in serviceConfiguration.ResolvedLinks **
                        value = stringVariableName;
                        // TODO: APPLY CONNECTION STRING TEMPLATE TO VALUE HERE

                        // TODO: Note for later: Remove /Configuration/* inputs when not using discovery
                        //       if we are verifying outputs/inputs in sequence

                        // apply this recent output value to any segment input which starts with
                        // /Configuration/* -- these special inputs represent receivers of underlying templates

                        foreach (var segmentInput in segment.RouteSegmentInputs.Where(c =>
                            ConfigurationInsertPattern.IsMatch(c)))
                        {
                            // /Configuration/<NameField>/<ValueField> will write the output to ValueField and its name match to NameField
                            var group = ConfigurationInsertPattern.Match(segmentInput);
                            var configNameField = group.Groups["name"].Value;
                            var configValueField = group.Groups["value"].Value;

                            var resolvedLinkName = serviceConfiguration.ResolvedOutgoingLinks.First()
                                .SourceLinkConfigurationName;
                            segment.TemplateConfigurationValues[configNameField] = resolvedLinkName;
                            segment.TemplateConfigurationValues[configValueField] = value;
                        }
                    }
                }
                else
                {
                    // don't add step if required inputs aren't present
                    if (generatedSegment.RouteSegmentInputs.Except(outputsAvailableSoFar).Any())
                        continue;

                    segmentsGeneratedThisPass.Add(generatedSegment);
                }

                allSegments.AddRange(segmentsGeneratedThisPass);
            }

            outputsAvailableSoFar.AddRange(allSegments.SelectMany(s => s.RouteSegmentOutputs));
            outputsAvailableSoFar = outputsAvailableSoFar.Distinct().ToList();

            return allSegments;
        }
    }

    public class RouteSegmentPossibility : List<WorkflowTemplateParticipantDefinition>
    {
        public List<string> RouteSegmentOutputs { get; set; } = new();
        public List<string> RouteSegmentInputs { get; set; } = new();

        public Dictionary<string, object> TemplateConfigurationValues { get; set; } = new();

        public RouteSegmentPossibility Clone()
        {
            var clone = new RouteSegmentPossibility();
            clone.AddRange(this);
            clone.RouteSegmentOutputs.AddRange(RouteSegmentOutputs);
            clone.RouteSegmentInputs.AddRange(RouteSegmentInputs);
            clone.TemplateConfigurationValues = new Dictionary<string, object>(TemplateConfigurationValues);
            return clone;
        }
    }
}