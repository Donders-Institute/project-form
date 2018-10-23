using System.Collections.Generic;
using JetBrains.Annotations;

namespace Dccn.ProjectForm.Configuration
{
    [UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
    public class LdapOptions
    {
        public const string SectionName = "Ldap";

        public string Host { get; set; }
        public ICollection<string> Hosts { get; set; }
        public int? Port { get; set; }

        public string Domain { get; set; }

        public bool UseSsl { get; set; }
    }
}
