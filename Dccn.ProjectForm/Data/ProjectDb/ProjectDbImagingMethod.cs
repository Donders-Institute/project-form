using JetBrains.Annotations;

namespace Dccn.ProjectForm.Data.ProjectDb
{
    [UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
    public class ProjectDbImagingMethod
    {
        public string Id { get; set; }
        public string Description { get; set; }
        public string ImagingGroup { get; set; }
        public string BillingCategory { get; set; }
        public string Url { get; set; }
        public int SessionQuota { get; set; }
    }
}
