using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SecOpsSteward.Plugins;
using SecOpsSteward.Shared.Cryptography.Extensions;

namespace SecOpsSteward.Shared.Messages
{
    /// <summary>
    ///     A collection of ExecutionSteps in order
    /// </summary>
    public class ExecutionStepCollection : List<ExecutionStep>
    {
        public ExecutionStepCollection()
        {
        }

        public ExecutionStepCollection(IEnumerable<ExecutionStep> steps)
        {
            AddRange(steps);
        }

        public List<ExecutionStep> GetPathToRoot(ExecutionStep item)
        {
            var items = new List<ExecutionStep>();
            var current = item;
            while (current != null)
            {
                items.Add(current);
                current = this.FirstOrDefault(i => i.StepId == current.ParentStepId);
            }

            return items;
        }

        public IEnumerable<ExecutionStep> GetNextSteps(Guid parentStepId, string resultCode = null)
        {
            return this.Where(s => s.ParentStepId == parentStepId &&
                                   (s.ParentStepResultCode == resultCode ||
                                    string.IsNullOrEmpty(s.ParentStepResultCode)));
        }

        public ExecutionStep AddStepWithoutSigning(
            ChimeraAgentIdentifier agentId,
            ChimeraPackageIdentifier packageId,
            byte[] packageSignature,
            string args)
        {
            var step = new ExecutionStep
            {
                StepId = Guid.NewGuid(),
                PackageId = packageId,
                Arguments = args,
                PackageSignature = packageSignature,
                RunningEntity = agentId
            };
            Add(step);
            return step;
        }

        public ExecutionStep AddStepWithoutSigning(
            ExecutionStep parent,
            ChimeraAgentIdentifier agentId,
            ChimeraPackageIdentifier packageId,
            byte[] packageSignature,
            string args)
        {
            var step = AddStepWithoutSigning(agentId, packageId, packageSignature, args);
            step.ParentStepId = parent.StepId;
            step.Conditions = new ExecutionStepConditions
            {
                // require all previous items from this path to match codes
                RequiredReceipts = GetConditionalReceiptsFromProgression(step)
            };
            return step;
        }

        public ExecutionStep AddStepWithoutSigning(
            ExecutionStep parent,
            string returnCode,
            ChimeraAgentIdentifier agentId,
            ChimeraPackageIdentifier packageId,
            byte[] packageSignature,
            string args)
        {
            var step = AddStepWithoutSigning(agentId, packageId, packageSignature, args);
            step.ParentStepId = parent.StepId;
            step.ParentStepResultCode = returnCode;
            step.Conditions = new ExecutionStepConditions
            {
                // require all previous items from this path to match codes
                RequiredReceipts = GetConditionalReceiptsFromProgression(parent)
            };
            return step;
        }

        public async Task<ExecutionStep> AddStep(
            ChimeraAgentIdentifier agentId,
            ChimeraPackageIdentifier packageId,
            byte[] packageSignature,
            string args,
            ICryptographicService cryptoService,
            ChimeraUserIdentifier signer,
            string signerDisplay)
        {
            var step = AddStepWithoutSigning(agentId, packageId, packageSignature, args);
            await step.Sign(cryptoService, signer, signerDisplay);
            return step;
        }

        public async Task<ExecutionStep> AddStep(
            ExecutionStep parent,
            ChimeraAgentIdentifier agentId,
            ChimeraPackageIdentifier packageId,
            byte[] packageSignature,
            string args,
            ICryptographicService cryptoService,
            ChimeraUserIdentifier signer,
            string signerDisplay)
        {
            var step = AddStepWithoutSigning(parent, agentId, packageId, packageSignature, args);
            await step.Sign(cryptoService, signer, signerDisplay);
            return step;
        }

        // this assumes ParentStepResultCode exists in the collection already...
        // need to establish pluginresults some other way...
        // [ persist resultcode as nonserialized somewhere? ]
        private List<ExecutionStepReceipt> GetConditionalReceiptsFromProgression(ExecutionStep executionStep)
        {
            return GetPathToRoot(executionStep).Select(i =>
            {
                if (i.ParentStepId != Guid.Empty) // skip on 0th entity
                    return new ExecutionStepReceipt
                    {
                        StepId = i.ParentStepId,
                        PluginResult = new PluginOutputStructure(i.ParentStepResultCode)
                    };
                return null;
            }).Where(r => r != null).ToList();
        }
    }
}