using System.Collections.Generic;
using JetBrains.Annotations;

namespace Dccn.ProjectForm.Configuration
{
    [UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
    public class FormOptions
    {
        public const string SectionName = "Form";

        public IDictionary<string, LabModality> Labs { get; set; }
        public AuthorityOptions Authorities { get; set; }

        public class LabModality
        {
            public string DisplayName { get; set; }
            public StorageAmount Storage { get; set; }
        }

        public class StorageAmount
        {
            public decimal? Fixed { get; set; }
            public decimal? Session { get; set; }
        }

        public class AuthorityOptions
        {
            public string Funding { get; set; }
            public string EthicalApproval { get; set; }
            public string LabCoordinatorMri { get; set; }
            public string LabCoordinatorOther { get; set; }
            public string PrivacyOfficer { get; set; }
            public string Director { get; set; }
            public string Administration { get; set; }
        }
    }
}
