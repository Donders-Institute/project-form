using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dccn.ProjectForm.Data;
using Dccn.ProjectForm.Models;

namespace Dccn.ProjectForm.Services.SectionHandlers
{
    public class EthicsSectionHandler : FormSectionHandlerBase<EthicsSectionModel>
    {
        public EthicsSectionHandler(IServiceProvider serviceProvider)
            : base(serviceProvider, m => m.Ethics)
        {
        }

        protected override IEnumerable<ApprovalAuthorityRole> ApprovalRoles => new []{ApprovalAuthorityRole.Ethics};

        protected override Task LoadAsync(EthicsSectionModel model, Proposal proposal)
        {
            if (proposal.EcApproved)
            {
                model.Status = EthicsApprovalStatusModel.Approved;
                switch (proposal.EcCode)
                {
                    case "CMO2014/288":
                        model.ApprovalCode = EthicsApprovalOptionModel.Blanket;
                        break;
                    case "CMO2012/012":
                        model.ApprovalCode = EthicsApprovalOptionModel.Children;
                        break;
                    default:
                        model.ApprovalCode = EthicsApprovalOptionModel.Other;
                        model.CustomCode = proposal.EcCode;
                        break;
                }
            }
            else
            {
                model.Status = EthicsApprovalStatusModel.Pending;
                model.CorrespondenceNumber = proposal.EcReference;
            }

            return base.LoadAsync(model, proposal);
        }

        protected override Task StoreAsync(EthicsSectionModel model, Proposal proposal)
        {
            switch (model.Status)
            {
                case EthicsApprovalStatusModel.Approved:
                    proposal.EcApproved = true;
                    switch (model.ApprovalCode)
                    {
                        case EthicsApprovalOptionModel.Blanket:
                            proposal.EcCode = "CMO2014/288";
                            break;
                        case EthicsApprovalOptionModel.Children:
                            proposal.EcCode = "CMO2012/012";
                            break;
                        case EthicsApprovalOptionModel.Other:
                            proposal.EcCode = model.CustomCode;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    proposal.EcReference = null;
                    break;
                case EthicsApprovalStatusModel.Pending:
                    proposal.EcApproved = false;
                    proposal.EcCode = null;
                    proposal.EcReference = model.CorrespondenceNumber;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return base.StoreAsync(model, proposal);
        }
    }
}