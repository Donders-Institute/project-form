using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Dccn.ProjectForm.Data.ProjectDb
{
    [UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
    public class ProjectDbProject
    {
        public string OldId { get; set; }
        public string Id { get; set; }
        public DateTime Created { get; set; }
        public DateTime Updated { get; set; }
        public string ProjectName { get; set; }
        public string OwnerId { get; set; }
        public string SourceId { get; set; }
        public DateTime? PpmDate { get; set; }
        public DateTime StartingDate { get; set; }
        public DateTime EndingDate { get; set; }
        public int PpmApprovedQuota { get; set; }
        public bool? EcApproved { get; set; }
        public string EcRequestedBy { get; set; }
        public string AuthorshipSequence { get; set; }
        public string AuthorshipRemarks { get; set; }
        public bool? ApprovedAdministration { get; set; }
        public bool? Sticky { get; set; }
        public bool? IncreaseQuota { get; set; }
        public DateTime? IncreaseDate { get; set; }
        public bool? Active { get; set; }
        public bool? ProjectBased { get; set; }
        public int UsedProjectSpace { get; set; }
        public int TotalProjectSpace { get; set; }
        public int CalculatedProjectSpace { get; set; }
        public DateTime ProjectEndDate { get; set; }

        public ICollection<ProjectDbProjectMember> ProjectMembers { get; set; }
        public ICollection<ProjectDbExperiment> Experiments { get; set; }
    }
}
