using System;
using JetBrains.Annotations;

namespace Dccn.ProjectForm.Data
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class Lab
    {
        public int Id { get; set; }

        public int ProposalId { get; private set; }

        public string Modality { get; set; }
        public int? SubjectCount { get; set; }
        public int? ExtraSubjectCount { get; set; }
        public int? SessionCount { get; set; }
        public TimeSpan? SessionDuration { get; set; }
    }
}
