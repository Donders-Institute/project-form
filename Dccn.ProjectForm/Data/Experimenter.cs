﻿using JetBrains.Annotations;

namespace Dccn.ProjectForm.Data
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class Experimenter
    {
        public int ProposalId { get; private set; }

        public string UserId { get; set; }
    }
}
