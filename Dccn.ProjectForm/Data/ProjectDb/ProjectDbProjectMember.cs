using System;
using JetBrains.Annotations;

namespace Dccn.ProjectForm.Data.ProjectDb
{
    [UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
    public class ProjectDbProjectMember
    {
        public ProjectDbUser User { get; set; }
        public string UserId { get; set; }
        public string ProjectId { get; set; }
        public DateTime Created { get; set; }
        public DateTime Updated { get; set; }
        public string Role { get; set; }
        public string Action { get; set; }
        public bool? Activated { get; set; }
    }
}