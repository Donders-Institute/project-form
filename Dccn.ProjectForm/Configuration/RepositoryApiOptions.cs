using JetBrains.Annotations;

namespace Dccn.ProjectForm.Configuration
{
    [UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
    public class RepositoryApiOptions
    {
        public const string SectionName = "DondersRepositoryApi";

        public string BaseUri { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
    }
}
