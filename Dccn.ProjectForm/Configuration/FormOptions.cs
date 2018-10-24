using System.Collections.Generic;
using Dccn.ProjectForm.Data;
using JetBrains.Annotations;

namespace Dccn.ProjectForm.Configuration
{
    [UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
    public class FormOptions
    {
        public const string SectionName = "Form";

        public IDictionary<string, LabModality> Labs { get; set; }
        public IDictionary<ApprovalAuthorityRole, string> Authorities { get; set; }

        public class LabModality
        {
            public string DisplayName { get; set; }
            public bool IsMri { get; set; }
            public StorageAmount Storage { get; set; }
        }

        public class StorageAmount
        {
            public decimal? Fixed { get; set; }
            public decimal? Session { get; set; }
        }
    }
}
