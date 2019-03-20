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
        private readonly ILabProvider _labProvider;

        public ExperimentSectionHandler(IServiceProvider serviceProvider, IUserManager userManager, ILabProvider labProvider)
            : base(serviceProvider, m => m.Experiment)
        {
            _userManager = userManager;
            _labProvider = labProvider;
        }

        protected override IEnumerable<ApprovalAuthorityRole> ApprovalRoles => new []
        {
            ApprovalAuthorityRole.LabMri, ApprovalAuthorityRole.LabOther
        };

        public override bool IsAuthorityApplicable(Proposal proposal, ApprovalAuthorityRole authorityRole)
        {
            var hasAnyMri = proposal.Labs.Any(l => _labProvider.Labs[l.Modality].IsMri);
            var hasAnyNonMri = proposal.Labs.Any(l => !_labProvider.Labs[l.Modality].IsMri);
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
                    Modality = _labProvider.Labs[lab.Modality],
                    SubjectCount = lab.SubjectCount,
                    ExtraSubjectCount = lab.ExtraSubjectCount,
                    SessionCount = lab.SessionCount,
                    SessionDurationMinutes = (int?) lab.SessionDuration?.TotalMinutes
                })
                .ToList();

            if (proposal.CustomQuota)
            {
                model.StorageQuota = StorageQuotaModel.Custom;
                model.CustomStorageQuota = proposal.CustomQuotaAmount;
                model.CustomStorageQuotaMotivation = proposal.CustomQuotaMotivation;
            }
            else
            {
                model.StorageQuota = StorageQuotaModel.Standard;
            }

            model.MinimumStorageQuota = _labProvider.MinimumStorageQuota;

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

            foreach (var labModel in model.Labs)
            {
                var lab = proposal.Labs.SingleOrDefault(l => l.Id == labModel.Id);
                if (lab == null)
                {
                    continue;
                }

                lab.Modality = labModel.Modality.Id;
                lab.SubjectCount = labModel.SubjectCount;
                lab.ExtraSubjectCount = labModel.ExtraSubjectCount;
                lab.SessionCount = labModel.SessionCount;
                lab.SessionDuration = labModel.SessionDurationMinutes.HasValue
                    ? TimeSpan.FromMinutes(labModel.SessionDurationMinutes.Value)
                    : (TimeSpan?) null;
            }

            if (model.StorageQuota == StorageQuotaModel.Standard)
            {
                proposal.CustomQuota = false;
                proposal.CustomQuotaAmount = null;
                proposal.CustomQuotaMotivation = null;
            }
            else
            {
                proposal.CustomQuota = true;
                proposal.CustomQuotaAmount = model.CustomStorageQuota;
                proposal.CustomQuotaMotivation = model.CustomStorageQuotaMotivation;
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