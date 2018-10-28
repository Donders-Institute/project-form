using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dccn.ProjectForm.Data;
using Dccn.ProjectForm.Data.Projects;
using Dccn.ProjectForm.Extensions;
using Dccn.ProjectForm.Models;
using Microsoft.EntityFrameworkCore;

namespace Dccn.ProjectForm.Services.SectionHandlers
{
    public class ExperimentHandler : FormSectionHandlerBase<ExperimentSectionModel>
    {
        private readonly ProjectsDbContext _projectsDbContext;
        private readonly IModalityProvider _modalityProvider;

        public ExperimentHandler(IAuthorityProvider authorityProvider, ProjectsDbContext projectsDbContext, IModalityProvider modalityProvider)
            : base(authorityProvider, m => m.Experiment)
        {
            _projectsDbContext = projectsDbContext;
            _modalityProvider = modalityProvider;
        }

        protected override IEnumerable<ApprovalAuthorityRole> ApprovalRoles => new []
        {
            ApprovalAuthorityRole.LabMri, ApprovalAuthorityRole.LabOther
        };

        protected override async Task LoadAsync(ExperimentSectionModel model, Proposal proposal, ProjectsUser owner, ProjectsUser supervisor)
        {
            model.StartDate = proposal.StartDate;
            model.EndDate = proposal.EndDate;

            model.Labs = proposal.Labs
                .Select(lab => new Models.LabModel
                {
                    Id = lab.Id,
                    Modality = _modalityProvider[lab.Modality],
                    SubjectCount = lab.SubjectCount,
                    ExtraSubjectCount = lab.ExtraSubjectCount,
                    SessionCount = lab.SessionCount,
                    SessionDurationMinutes = (int?) lab.SessionDuration?.TotalMinutes
                })
                .ToDictionary(_ => Guid.NewGuid());

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
                .Select(async experimenter => new UserModel
                {
                    Id = experimenter.UserId,
                    Name = (await _projectsDbContext.Users.FirstOrDefaultAsync(u => u.Id == experimenter.UserId)).DisplayName
                })
                .ToDictionaryAsync(_ => Guid.NewGuid());

            await base.LoadAsync(model, proposal, owner, supervisor);
        }

        protected override Task StoreAsync(ExperimentSectionModel model, Proposal proposal)
        {
            proposal.StartDate = model.StartDate;
            proposal.EndDate = model.EndDate;

            proposal.Labs = (model.Labs?.Values ?? Enumerable.Empty<Models.LabModel>())
                .Select(lab => new Data.Lab
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

            proposal.Experimenters = (model.Experimenters?.Values ?? Enumerable.Empty<UserModel>())
                .Select( experimenter => new Experimenter
                {
                    UserId = experimenter.Id
                })
                .ToList();

            return base.StoreAsync(model, proposal);
        }

        protected override bool IsAuthorityApplicable(Proposal proposal, ApprovalAuthorityRole authorityRole)
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
    }
}