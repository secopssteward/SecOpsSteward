using Blazor.Diagrams.Core;
using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models;
using SecOpsSteward.Data.Models;
using SecOpsSteward.Data.Workflow;
using SecOpsSteward.UI.Pages.Workflows.Composer.Links;
using SecOpsSteward.UI.Pages.Workflows.Composer.Nodes;
using System.Collections.Generic;
using System.Linq;

namespace SecOpsSteward.UI.Pages.Workflows.Composer
{
    public static class SavedWorkflowExtensions
    {
        public static SavedWorkflow SaveWorkflow(this Diagram diagram)
        {
            var savedWf = new SavedWorkflow();
            savedWf.Nodes = diagram.Nodes.Cast<WorkflowComposerNode>().Select(model =>
                new SavedNode()
                {
                    Id = model.Id,
                    X = model.Position.X,
                    Y = model.Position.Y,
                    W = model.Size.Width,
                    H = model.Size.Height,
                    AgentId = model.AgentId,
                    PackageId = model.Package.PluginId,
                    WorkflowStepId = model.WorkflowStepId,
                    NodeName = model.NodeName,
                    Parameters = model.Parameters
                }).ToList();
            savedWf.Links = diagram.Links.Select(link =>
                new SavedLink()
                {
                    Id = link.Id,
                    SourceNodeId = link.SourceNode.Id,
                    TargetNodeId = link.TargetPort.Parent.Id,
                    SourceOutputCode = (link.SourcePort as OutputPort).OutputCode
                }).ToList();

            return savedWf;
        }

        public static void LoadWorkflow(this Diagram diagram, SavedWorkflow workflow, IEnumerable<PluginMetadataModel> packages)
        {
            foreach (var node in workflow.Nodes)
                diagram.Nodes.Add(AsWorkflowComposerNode(node, packages));
            foreach (var link in workflow.Links)
            {
                var linkModels = new List<LinkModel>();
                var source = diagram.Nodes.First(n => n.Id == link.SourceNodeId);
                var sourcePort = source.Ports.OfType<OutputPort>().First(p => p.OutputCode == link.SourceOutputCode);
                var target = diagram.Nodes.First(n => n.Id == link.TargetNodeId);
                var targetPort = target.Ports.OfType<InputPort>().First();

                diagram.Links.Add(new LinkModel(link.Id, sourcePort, targetPort));
            }
        }

        public static WorkflowComposerNode AsWorkflowComposerNode(this SavedNode node, IEnumerable<PluginMetadataModel> packages)
        {
            var wfNode = new WorkflowComposerNode(
                node.Id,
                packages.First(p => p.PluginId == node.PackageId),
                node.X, node.Y);
            wfNode.NodeName = node.NodeName;
            wfNode.AgentId = node.AgentId;
            wfNode.Size = new Size(node.W, node.H);
            wfNode.Parameters = node.Parameters.Clone();
            wfNode.WorkflowStepId = node.WorkflowStepId;
            return wfNode;
        }
    }
}
