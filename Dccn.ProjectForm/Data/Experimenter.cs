using JetBrains.Annotations;

namespace Dccn.ProjectForm.Data
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class Experimenter
    {
        public int Id { get; private set; }

        public int ProposalId { get; private set; }

        public UserReference User { get; set; }
    }
}
