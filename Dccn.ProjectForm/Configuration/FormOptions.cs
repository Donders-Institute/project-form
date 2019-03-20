using System.Collections.Generic;
using Dccn.ProjectForm.Data;
using JetBrains.Annotations;

namespace Dccn.ProjectForm.Configuration
{
    [UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
    public class FormOptions
    {
        public const string SectionName = "Form";

        public IDictionary<string, string> EthicalCodes { get; set; }
        public LabOptions Labs { get; set; }
        public IDictionary<ApprovalAuthorityRole, ICollection<string>> Authorities { get; set; }
        public ICollection<string> Administration { get; set; }
        public ICollection<string> Admins { get; set; }
        public PrivacyOptions Privacy { get; set; }

        [UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
        public class LabOptions
        {
            public IDictionary<string, string> Modalities { get; set; }
            public int MinimumStorageQuota { get; set; }
        }

        [UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
        public class LabModality
        {
            public string DisplayName { get; set; }
            public bool IsMri { get; set; }
            public int? SessionStorageQuota { get; set; }
        }

        [UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
        public class PrivacyOptions
        {
            public IDictionary<string, string> DataTypes { get; set; }
            public IDictionary<string, string> Motivations { get; set; }
            public IDictionary<string, string> StorageLocations { get; set; }
            public IDictionary<string, string> DataAccessors { get; set; }
            public IDictionary<string, string> DataDisposalTerms { get; set; }
        }
    }
}
