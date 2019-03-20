using System.Collections.Generic;
using JetBrains.Annotations;

namespace Dccn.ProjectForm.Data.ProjectDb
{
    [UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
    public class ProjectDbGroup
    {
        public string Id { get; private set; }

        public ProjectDbUser Head { get; private set; }
        public string HeadId { get; private set; }
        public bool HeadIsPi { get; private set; }

        public string Description { get; private set; }
        public bool Hidden { get; private set; }
        public string InstituteId { get; private set; }

        public ICollection<ProjectDbUser> Members { get; private set; }
    }
}
