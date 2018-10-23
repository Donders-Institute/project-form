using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dccn.ProjectForm.Data;
using Dccn.ProjectForm.Data.Projects;
using Dccn.ProjectForm.Models;

namespace Dccn.ProjectForm.Services.SectionHandlers
{
    public class EthicsHandler : FormSectionHandlerBase<Ethics>
    {
        public EthicsHandler(IAuthorityProvider authorityProvider) : base(authorityProvider)
        {
        }

        protected override IEnumerable<ApprovalAuthorityRole> ApprovalRoles => new []{ApprovalAuthorityRole.Ethics};

        public override Task LoadAsync(Ethics model, Proposal proposal, ProjectsUser owner, ProjectsUser supervisor)
        {
            if (proposal.EcApproved)
            {
                model.Approved = true;
                switch (proposal.EcCode)
                {
                    case "CMO2014/288":
                        model.ApprovalCode = Ethics.ApprovalType.Blanket;
                        break;
                    case "CMO2012/012":
                        model.ApprovalCode = Ethics.ApprovalType.Children;
                        break;
                    default:
                        model.ApprovalCode = Ethics.ApprovalType.Other;
                        model.CustomCode = proposal.EcCode;
                        break;
                }
            }
            else
            {
                model.Approved = false;
                model.CorrespondenceNumber = proposal.EcReference;
            }

            return base.LoadAsync(model, proposal, owner, supervisor);
        }

        public override Task StoreAsync(Ethics model, Proposal proposal)
        {
            if (model.Approved)
            {
                proposal.EcApproved = true;
                switch (model.ApprovalCode)
                {
                    case Ethics.ApprovalType.Blanket:
                        proposal.EcCode = "CMO2014/288";
                        break;
                    case Ethics.ApprovalType.Children:
                        proposal.EcCode = "CMO2012/012";
                        break;
                    case Ethics.ApprovalType.Other:
                        proposal.EcCode = model.CustomCode;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                proposal.EcReference = null;
            }
            else
            {
                proposal.EcApproved = false;
                proposal.EcCode = null;
                proposal.EcReference = model.CorrespondenceNumber;
            }

            return base.StoreAsync(model, proposal);
        }
    }
}