using System;
using JetBrains.Annotations;

namespace Dccn.ProjectForm.Data.ProjectDb
{
    [UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
    public class ProjectDbExperiment
    {
        public int Id { get; set; }
        public string Suffix { get; set; }
        public string ProjectOldId { get; set; }
        public string ProjectId { get; set; }
        public DateTime Created { get; set; }
        public DateTime Updated { get; set; }
        public string QuotaUserId { get; set; }
        public string ExperimenterId { get; set; }
        public DateTime StartingDate { get; set; }
        public DateTime EndingDate { get; set; }
        public int NoSubjects { get; set; }
        public int NoSessions { get; set; }
        public string ImagingMethodId { get; set; }
        public int LabTime { get; set; }
        public int? PpmApprovedQuota { get; set; }
        public int CalculatedQuota { get; set; }
        public DateTime? IncreaseQuotaDate { get; set; }
        public int IncreasedQuota { get; set; }
        public int Offset { get; set; }
        public DateTime ExperimentEndDate { get; set; }

        public ProjectDbUser QuotaUser { get; set; }
        public ProjectDbUser Experimenter { get; set; }
        public ProjectDbImagingMethod ImagingMethod { get; set; }
    }
}
