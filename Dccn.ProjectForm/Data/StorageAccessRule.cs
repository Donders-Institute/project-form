using JetBrains.Annotations;

namespace Dccn.ProjectForm.Data
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class StorageAccessRule
    {
        public int ProposalId { get; private set; }

        public string UserId { get; set; }
        public StorageAccessRole Role { get; set; }
    }

    public enum StorageAccessRole
    {
        Manager, Contributor, Viewer
    }
}
