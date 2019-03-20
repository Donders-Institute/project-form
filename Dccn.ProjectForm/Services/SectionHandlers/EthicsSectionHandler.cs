using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dccn.ProjectForm.Configuration;
using Dccn.ProjectForm.Data;
using Dccn.ProjectForm.Models;
using Microsoft.Extensions.Options;

namespace Dccn.ProjectForm.Services.SectionHandlers
{
    public class EthicsSectionHandler : FormSectionHandlerBase<EthicsSectionModel>
    {
        private readonly FormOptions _formOptions;

        public EthicsSectionHandler(IServiceProvider serviceProvider, IOptionsSnapshot<FormOptions> formOptions)
            : base(serviceProvider, m => m.Ethics)
        {
            _formOptions = formOptions.Value;
        }

        protected override IEnumerable<ApprovalAuthorityRole> ApprovalRoles => new []{ApprovalAuthorityRole.Ethics};

        protected override Task LoadAsync(EthicsSectionModel model, Proposal proposal)
        {
            if (proposal.EcApproved)
            {
                model.Status = EthicsApprovalStatusModel.Approved;
                model.Code = proposal.EcCode;
            }
            else
            {
                model.Status = EthicsApprovalStatusModel.Pending;
                model.CorrespondenceNumber = proposal.EcReference;
            }

            model.Codes = _formOptions.EthicalCodes;
//                .Select(entry => new EthicsApprovalOptionModel
//                {
//                    Name = entry.Key,
//                    Code = entry.Value
//                })
//                .OrderBy(option => option.Name)
//                .ToList();

            return base.LoadAsync(model, proposal);
        }

        protected override Task StoreAsync(EthicsSectionModel model, Proposal proposal)
        {
            switch (model.Status)
            {
                case EthicsApprovalStatusModel.Approved:
                    proposal.EcApproved = true;
                    proposal.EcCode = model.Code;
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