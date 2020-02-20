using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Dccn.ProjectForm.Data.ProjectDb
{
    [UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
    public class ProjectDbFundingSource
    {
        public string OldId { get; set; }
        public string Id { get; set; }
        public string Description { get; set; }
    }
}
