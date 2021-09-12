using SecOpsSteward.Data.Models;
using SecOpsSteward.Data.Workflow;
using SecOpsSteward.Shared;
using SecOpsSteward.Shared.Messages;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SecOpsSteward.UI.Pages.Workflows
{
    public class SavedNodeWithLink : SavedNode
    {
        public Dictionary<string, List<SavedNodeWithLink>> LinksOut { get; set; } =
            new Dictionary<string, List<SavedNodeWithLink>>();

        public ExecutionStep AuthorizingMessage { get; set; }

        public SavedNodeWithLink(SavedNode n)
        {
            this.Id = n.Id;
            this.AgentId = n.AgentId;
            this.PackageId = n.PackageId;
            this.Parameters = n.Parameters;
            this.WorkflowStepId = n.WorkflowStepId;
        }
    }

    public class StepSequencer
    {
        private List<SavedNodeWithLink> _nodesWithLinks = new List<SavedNodeWithLink>();
        private SavedWorkflow _saved;

        public StepSequencer(WorkflowModel model)
        {
            _saved = ChimeraSharedHelpers.GetFromSerializedString<SavedWorkflow>(model.WorkflowJson);
            _nodesWithLinks = _saved.Nodes.Select(n => new SavedNodeWithLink(n)).ToList();
            foreach (var n in _nodesWithLinks)
            {
                var linksOut = _saved.Links.Where(l => l.SourceNodeId == n.Id);
                foreach (var l in linksOut)
                {
                    if (!n.LinksOut.ContainsKey(l.SourceOutputCode))
                        n.LinksOut[l.SourceOutputCode] = new List<SavedNodeWithLink>();
                    var target = _saved.Nodes.First(n => n.Id == l.TargetNodeId);
                    n.LinksOut[l.SourceOutputCode].Add(_nodesWithLinks.First(n => n.Id == target.Id));
                }
            }
        }

        /// <summary>
        /// Get an ExecutionStepCollection which represents the step hierarchy and order for this workflow
        /// </summary>
        /// <returns></returns>
        public ExecutionStepCollection CreateStepCollectionFromWorkflow()
        {
            var step = GetRoot();
            var steps = new ExecutionStepCollection();
            var parent = steps.AddStepWithoutSigning(step.AgentId, step.PackageId, null, ChimeraSharedHelpers.SerializeToString(step.Parameters.AsDictionary()));
            step.AuthorizingMessage = parent;
            foreach (var next in step.LinksOut)
                AddStepsToCollection(steps, parent, next);
            return steps;
        }
        private void AddStepsToCollection(ExecutionStepCollection steps, ExecutionStep parentStep, KeyValuePair<string, List<SavedNodeWithLink>> lastOutputs)
        {
            foreach (var thisStep in lastOutputs.Value)
            {
                var parent = steps.AddStepWithoutSigning(parentStep, lastOutputs.Key, thisStep.AgentId, thisStep.PackageId, null, ChimeraSharedHelpers.SerializeToString(thisStep.Parameters.AsDictionary()));
                thisStep.AuthorizingMessage = parent;
                foreach (var next in thisStep.LinksOut)
                    AddStepsToCollection(steps, parent, next);
            }
        }
        private SavedNodeWithLink GetRoot()
        {
            var allLinkTargets = _nodesWithLinks.SelectMany(l => l.LinksOut.SelectMany(lo => lo.Value.Select(v => v.Id)));
            var noLinksIn = _nodesWithLinks.Where(l => !allLinkTargets.Contains(l.Id));
            return noLinksIn.FirstOrDefault();
        }
    }
}
