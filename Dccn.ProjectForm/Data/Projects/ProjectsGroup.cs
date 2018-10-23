using System.Collections.Generic;
using JetBrains.Annotations;

namespace Dccn.ProjectForm.Data.Projects
{
    [UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
    public class ProjectsGroup
    {
        public string Id { get; private set; }

        public ProjectsUser Head { get; private set; }
        public string HeadId { get; private set; }
        public bool HeadIsPi { get; private set; }

        public string Description { get; private set; }
        public bool Hidden { get; private set; }
        public string InstituteId { get; private set; }

        public ICollection<ProjectsUser> Members { get; private set; }
    }
}
