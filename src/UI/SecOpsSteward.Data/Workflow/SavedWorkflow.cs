using System.Collections.Generic;

namespace SecOpsSteward.Data.Workflow
{
    public class SavedWorkflow
    {
        public List<SavedLink> Links { get; set; } = new List<SavedLink>();
        public List<SavedNode> Nodes { get; set; } = new List<SavedNode>();
    }
}
