using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dccn.ProjectForm.Authentication;
using Dccn.ProjectForm.Data;
using Dccn.ProjectForm.Extensions;
using Dccn.ProjectForm.Models;

namespace Dccn.ProjectForm.Services.SectionHandlers
{
    public class ExperimentSectionHandler : FormSectionHandlerBase<ExperimentSectionModel>
    {
        private readonly IUserManager _userManager;
        private readonly IModalityProvider _modalityProvider;

        public ExperimentSectionHandler(IServiceProvider serviceProvider, IUserManager userManager, IModalityProvider modalityProvider)
            : base(serviceProvider, m => m.Experiment)
        {
            _userManager = userManager;
            _modalityProvider = modalityProvider;
        }

        protected override IEnumerable<ApprovalAuthorityRole> ApprovalRoles => new []
        {
            ApprovalAuthorityRole.LabMri, ApprovalAuthorityRole.LabOther
        };

        public override bool IsAuthorityApplicable(Proposal proposal, ApprovalAuthorityRole authorityRole)
        {
            var hasAnyMri = proposal.Labs.Any(l => _modalityProvider[l.Modality].IsMri);
            var hasAnyNonMri = proposal.Labs.Any(l => !_modalityProvider[l.Modality].IsMri);
            switch (authorityRole)
            {
                case ApprovalAuthorityRole.LabMri:
                    return hasAnyMri;
                case ApprovalAuthorityRole.LabOther:
                    return hasAnyNonMri || !hasAnyMri;
                default:
                    throw new ArgumentOutOfRangeException(nameof(authorityRole), authorityRole, null);
            }
        }

        protected override async Task LoadAsync(ExperimentSectionModel model, Proposal proposal)
        {
            model.StartDate = proposal.StartDate;
            model.EndDate = proposal.EndDate;

            model.Labs = proposal.Labs
                .Select(lab => new LabModel
                {
                    Id = lab.Id,
                    Modality = _modalityProvider[lab.Modality],
                    SubjectCount = lab.SubjectCount,
                    ExtraSubjectCount = lab.ExtraSubjectCount,
                    SessionCount = lab.SessionCount,
                    SessionDurationMinutes = (int?) lab.SessionDuration?.TotalMinutes
                })
                .ToList();

            if (proposal.CustomQuota)
            {
                model.StorageQuota = StorageQuotaModel.Custom;
                model.CustomQuotaAmount = proposal.CustomQuotaAmount;
                model.CustomQuotaMotivation = proposal.CustomQuotaMotivation;
            }
            else
            {
                model.StorageQuota = StorageQuotaModel.Standard;
            }

            model.Experimenters = await proposal.Experimenters
                .Select(async experimenter => new ExperimenterModel
                {
                    Id = experimenter.UserId,
                    Name = (await _userManager.GetUserByIdAsync(experimenter.UserId)).DisplayName
                })
                .ToListAsync();

            await base.LoadAsync(model, proposal);
        }

        protected override Task StoreAsync(ExperimentSectionModel model, Proposal proposal)
        {
            proposal.StartDate = model.StartDate;
            proposal.EndDate = model.EndDate;

            proposal.Labs = model.Labs
                .Select(lab => new Lab
                {
                    // FIXME: causes DELETE + INSERT instead of UPDATE
                    // Id = lab.Id.GetValueOrDefault(),
                    Modality = lab.Modality.Id,
                    SubjectCount = lab.SubjectCount,
                    ExtraSubjectCount = lab.ExtraSubjectCount,
                    SessionCount = lab.SessionCount,
                    SessionDuration = lab.SessionDurationMinutes.HasValue ? TimeSpan.FromMinutes(lab.SessionDurationMinutes.Value) : (TimeSpan?) null
                })
                .ToList();

            if (model.StorageQuota == StorageQuotaModel.Standard)
            {
                proposal.CustomQuota = false;
                proposal.CustomQuotaAmount = null;
                proposal.CustomQuotaMotivation = null;
            }
            else
            {
                proposal.CustomQuota = true;
                proposal.CustomQuotaAmount = model.CustomQuotaAmount;
                proposal.CustomQuotaMotivation = model.CustomQuotaMotivation;
            }

            proposal.Experimenters = model.Experimenters
                .Select(experimenter => new Experimenter
                {
                    UserId = experimenter.Id
                })
                .ToList();

            return base.StoreAsync(model, proposal);
        }
    }
}